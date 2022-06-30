using Node.P2P.ResponseModels;
using System.Text.Json;

namespace Node.P2P;

internal record UploadSession(
    FileInfo File,
    string FileId,
    string FileName,
    long UploadedBytesCount,
    UploadedPacket[] UploadedPackets,
    string Hostname,
    RequestOptions RequestOptions) : IAsyncDisposable
{
    bool _finalized;
    IEnumerable<Range>? _notUploadedByteRanges;
    internal IEnumerable<Range> NotUploadedByteRanges
    {
        get
        {
            if (_notUploadedByteRanges is not null) return _notUploadedByteRanges;
            if (!UploadedPackets.Any()) return _notUploadedByteRanges = new Range[] { new(0, (Index)File.Length) };

            var notUploadedByteRanges = new List<Range>();
            var controlOffset = 0;

            if (UploadedPackets.First().Offset != controlOffset)
            {
                notUploadedByteRanges.Add(new(controlOffset, (Index)UploadedPackets.First().Offset));
                controlOffset = (int)UploadedPackets.First().Offset;
            }
            for (var gb = 0; gb < UploadedPackets.Length - 1; gb++)
            {
                if (UploadedPackets[gb + 1].Offset == (controlOffset += UploadedPackets[gb].Length)) continue;

                notUploadedByteRanges.Add(new(
                    controlOffset,
                    controlOffset += (int)UploadedPackets[gb + 1].Offset - controlOffset
                    )
                );
            }
            if (File.Length != (controlOffset += UploadedPackets.Last().Length))
            {
                notUploadedByteRanges.Add(new(controlOffset, (Index)File.Length));
            }

            return notUploadedByteRanges;
        }
    }

    internal async Task<bool> EnsureAllBytesUploadedAsync()
        => (await InitializeAsync(File, RequestOptions).ConfigureAwait(false)).UploadedBytesCount == File.Length;

    internal static async Task<UploadSession> InitializeAsync(string filePath, RequestOptions? requestOptions = null)
        => await InitializeAsync(new FileInfo(filePath), requestOptions);

    internal static async Task<UploadSession> InitializeAsync(FileInfo file, RequestOptions? requestOptions = null)
    {
        requestOptions ??= new();

        var cWebUploadInfo = new
        {
            name = file.Name,
            size = file.Length,
            lastModified = file.LastWriteTimeUtc.ToBinary(),
            type = "video/mp4",
            uploadDirectory = file.Directory?.Name ?? "/"
        };

        var urlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            ["sid"] = Settings.SessionId!,
            ["fileinfo"] = JsonSerializer.Serialize(cWebUploadInfo)
        });

        var response = await Api.TrySendRequestAsync(
            async () => await requestOptions.HttpClient.PostAsync(
                "https://microstock.plus/api/1.0/content/upload/init",
                urlEncodedContent,
                requestOptions.CancellationToken).ConfigureAwait(false),
            requestOptions).ConfigureAwait(false);
        var rawJsonResponse = await response.Content.ReadAsStringAsync(requestOptions.CancellationToken).ConfigureAwait(false);
        var jsonElementResponse = JsonDocument.Parse(rawJsonResponse).RootElement;
        return new(
            file,
            jsonElementResponse.GetProperty("fileId").GetString()!,
            jsonElementResponse.GetProperty("fileName").GetString()!,
            jsonElementResponse.GetProperty("uploadedbytes").GetInt64(),
            jsonElementResponse.GetProperty("uploadedchunks")
                .Deserialize<UploadedPacket[]>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!,
            jsonElementResponse.GetProperty("hostname").GetString()!,
            requestOptions);
    }

    public async ValueTask DisposeAsync()
    {
        await FinalizeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    internal async ValueTask FinalizeAsync()
    {
        if (_finalized) return;

        await Api.TrySendRequestAsync(
            async () => await RequestOptions.HttpClient.PostAsync(
                $"https://{Hostname}/content/vcupload/finish",
                new FormUrlEncodedContent(new Dictionary<string, string>() { ["fileid"] = FileId }),
                RequestOptions.CancellationToken).ConfigureAwait(false),
            RequestOptions).ConfigureAwait(false);
        _finalized = true;
    }

    //async Task<string> RegisterTaskAsync(FileInfo file)
    //{
    //    var httpContent = new MultipartFormDataContent()
    //    {
    //        { new StringContent(Guid.NewGuid().ToString()!), "sessionid" },
    //        { JsonContent.Create(new { filename = file.Name, size = file.Length }) },
    //        { JsonContent.Create(new { type = "MPlus", iid = Guid.NewGuid().ToString() }) },
    //        { JsonContent.Create(new { type = "Mplus", name = file.Name, directory = file.Directory?.Name ?? "/" }) },
    //        { JsonContent.Create(new { cTMTaskDataStub = "Stub", cTMTaskDataEditVideo = "EditVideo", cTMTaskDataEditRaster = "EditRaster" }) },
    //        { new StringContent(string.Empty), "origin" }
    //    };

    //    var response = await TrySendRequestAsync(
    //        () => _requestOptions.HttpClient.PostAsync("https://microstock.plus/rphtaskmgr/registermytask", httpContent)
    //        );
    //    var jsonResponse = await response.Content.ReadAsStringAsync(_requestOptions.CancellationToken);

    //    return JsonDocument.Parse(jsonResponse).RootElement.GetProperty("taskid").GetString()!;
    //}
}
