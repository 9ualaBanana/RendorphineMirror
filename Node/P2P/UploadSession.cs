using Node.P2P.Models;
using System.Text.Json;

namespace Node.P2P;

//Currently supports files of maximum length no more than int.MaxValue.
internal record UploadSession(
    UploadSessionData Data,
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
            if (!UploadedPackets.Any()) return _notUploadedByteRanges = new Range[] { new(0, (Index)Data.File.Length) };

            var notUploadedByteRanges = new List<Range>();
            var controlOffset = 0;

            if (UploadedPackets.First().Offset != controlOffset)
            {
                notUploadedByteRanges.Add(new(controlOffset, (Index)UploadedPackets.First().Offset));
                controlOffset = (int)UploadedPackets.First().Offset;
            }
            for (var g = 0; g < UploadedPackets.Length - 1; g++)
            {
                if (UploadedPackets[g + 1].Offset == (controlOffset += UploadedPackets[g].Length)) continue;

                notUploadedByteRanges.Add(new(
                    controlOffset,
                    controlOffset += (int)UploadedPackets[g + 1].Offset - controlOffset
                    )
                );
            }
            if (Data.File.Length != (controlOffset += UploadedPackets.Last().Length))
            {
                notUploadedByteRanges.Add(new(controlOffset, (Index)Data.File.Length));
            }

            return notUploadedByteRanges;
        }
    }

    internal async Task<bool> EnsureAllBytesUploadedAsync() =>
        (await InitializeAsync(Data, RequestOptions).ConfigureAwait(false))
        .UploadedBytesCount == Data.File.Length;

    internal static async Task<UploadSession> InitializeAsync(
        UploadSessionData sessionData,
        RequestOptions? requestOptions = null)
    {
        requestOptions ??= new();

        var httpResponse = await Api.TryPostAsync(
            sessionData.Endpoint,
            sessionData.HttpContent,
            requestOptions).ConfigureAwait(false);
        var rawJsonResponse = await httpResponse.Content.ReadAsStringAsync(requestOptions.CancellationToken).ConfigureAwait(false);
        var response = JsonDocument.Parse(rawJsonResponse).RootElement;
        return new(
            sessionData,
            response.GetProperty("fileid").GetString()!,
            response.GetProperty("host").GetString()!,
            response.GetProperty("uploadedbytes").GetInt64(),
            response.GetProperty("uploadedchunks")
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

        await Api.TryPostAsync(
            $"https://{Host}/content/vcupload/finish",
            new FormUrlEncodedContent(new Dictionary<string, string>() { ["fileid"] = FileId }),
            RequestOptions).ConfigureAwait(false);

        _finalized = true;
    }
}
