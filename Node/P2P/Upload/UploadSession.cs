using Node.P2P.Models;
using System.Text.Json;

namespace Node.P2P.Upload;

internal record UploadSession(
    UploadSessionData Data,
    string FileId,
    string Host,
    long UploadedBytesCount,
    UploadedPacket[] UploadedPackets,
    HttpClient HttpClient,
    CancellationToken CancellationToken) : IAsyncDisposable
{
    bool _finalized;
    IEnumerable<LongRange>? _notUploadedBytes;
    internal IEnumerable<LongRange> NotUploadedBytes
    {
        get
        {
            if (_notUploadedBytes is not null) return _notUploadedBytes;

            if (!UploadedPackets.Any()) return _notUploadedBytes = new LongRange[] { new(0, Data.File.Length) };

            var notUploadedBytes = new List<LongRange>();
            long controlOffset = 0L;

            if (UploadedPackets.First().Offset != controlOffset)
            {
                notUploadedBytes.Add(new(controlOffset, UploadedPackets.First().Offset));
                controlOffset = UploadedPackets.First().Offset;
            }
            for (var g = 0; g < UploadedPackets.Length - 1; g++)
            {
                if (UploadedPackets[g + 1].Offset == (controlOffset += UploadedPackets[g].Length)) continue;

                notUploadedBytes.Add(
                    new(controlOffset, controlOffset += UploadedPackets[g + 1].Offset - controlOffset)
                    );
            }
            if (Data.File.Length != (controlOffset += UploadedPackets.Last().Length))
            {
                notUploadedBytes.Add(new(controlOffset, Data.File.Length));
            }

            return notUploadedBytes;
        }
    }

    internal async Task<bool> EnsureAllBytesUploadedAsync() =>
        (await InitializeAsync(Data, HttpClient, CancellationToken).ConfigureAwait(false))
        .UploadedBytesCount == Data.File.Length;

    internal static async Task<UploadSession> InitializeAsync(
        UploadSessionData sessionData, HttpClient httpClient, CancellationToken cancellationToken)
    {
        var httpResponse = await httpClient.PostAsync(sessionData.Endpoint, sessionData.HttpContent, cancellationToken).ConfigureAwait(false);
        var rawJsonResponse = await httpResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var response = JsonDocument.Parse(rawJsonResponse).RootElement;
        return new(
            sessionData,
            response.GetProperty("fileid").GetString()!,
            response.GetProperty("host").GetString()!,
            response.GetProperty("uploadedbytes").GetInt64(),
            response.GetProperty("uploadedchunks").Deserialize<UploadedPacket[]>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!,
            httpClient,
            cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await FinalizeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    internal async ValueTask FinalizeAsync()
    {
        if (_finalized) return;

        await HttpClient.PostAsync(
            $"https://{Host}/content/vcupload/finish",
            new FormUrlEncodedContent(new Dictionary<string, string>() { ["fileid"] = FileId })
            ).ConfigureAwait(false);

        _finalized = true;
    }
}
