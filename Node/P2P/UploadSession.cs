using Node.P2P.ResponseModels;
using System.Text.Json;

namespace Node.P2P;

internal record UploadSession(
    FileInfo File,
    string TaskId,
    string FileId,
    string Host,
    long UploadedBytesCount,
    UploadedPacket[] UploadedPackets,
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
        => (await InitializeAsync(File, TaskId, RequestOptions).ConfigureAwait(false)).UploadedBytesCount == File.Length;

    internal static async Task<UploadSession> InitializeAsync(string filePath, string taskId, RequestOptions? requestOptions = null)
        => await InitializeAsync(new FileInfo(filePath), taskId, requestOptions);

    internal static async Task<UploadSession> InitializeAsync(FileInfo file, string taskId, RequestOptions? requestOptions = null)
    {
        requestOptions ??= new();

        var urlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            ["sessionid"] = Settings.SessionId!,
            ["taskid"] = taskId,
            ["fsize"] = file.Length.ToString(),
            ["mimetype"] = "video/mp4",
            ["lastmodified"] = file.LastWriteTimeUtc.ToBinary().ToString(),
            ["origin"] = string.Empty
        });

        var response = await Api.TrySendRequestAsync(
            async () => await requestOptions.HttpClient.PostAsync(
                $"{Api.TaskManagerEndpoint}/initmptaskoutput",
                urlEncodedContent,
                requestOptions.CancellationToken).ConfigureAwait(false),
            requestOptions).ConfigureAwait(false);
        var rawJsonResponse = await response.Content.ReadAsStringAsync(requestOptions.CancellationToken).ConfigureAwait(false);
        var jsonElementResponse = JsonDocument.Parse(rawJsonResponse).RootElement;
        return new(
            file,
            taskId,
            jsonElementResponse.GetProperty("fileid").GetString()!,
            jsonElementResponse.GetProperty("host").GetString()!,
            jsonElementResponse.GetProperty("uploadedbytes").GetInt64(),
            jsonElementResponse.GetProperty("uploadedchunks")
                .Deserialize<UploadedPacket[]>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!,
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
                $"https://{Host}/content/vcupload/finish",
                new FormUrlEncodedContent(new Dictionary<string, string>() { ["fileid"] = FileId }),
                RequestOptions.CancellationToken).ConfigureAwait(false),
            RequestOptions).ConfigureAwait(false);
        _finalized = true;
    }
}
