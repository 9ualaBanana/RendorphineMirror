using Common;
using NLog;
using Transport.Models;

namespace Transport.Upload;

internal record UploadSession(
    UploadSessionData Data,
    string FileId,
    string Host,
    long UploadedBytesCount,
    UploadedPacket[] UploadedPackets,
    HttpClient HttpClient,
    CancellationToken CancellationToken) : IAsyncDisposable
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    string? _iid;
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

    internal async Task<bool> EnsureAllBytesUploadedAsync()
    {
        bool allAreUploaded = (await InitializeAsync(Data, HttpClient, CancellationToken).ConfigureAwait(false))
            .UploadedBytesCount == Data.File.Length;

        if (allAreUploaded) _logger.Debug("All bytes were successfully uploaded");
        else _logger.Warn("Not all bytes were successfully uploaded");

        return allAreUploaded;
    }

    internal static async Task<UploadSession> InitializeAsync(
        UploadSessionData sessionData, HttpClient httpClient, CancellationToken cancellationToken)
    {
        _logger.Debug("Initializing upload session...");
        var uploadSession = await InitializeOrThrowAsync(sessionData, httpClient, cancellationToken).ConfigureAwait(false);
        _logger.Debug("Upload session is initialized");
        return uploadSession;
    }

    static async Task<UploadSession> InitializeOrThrowAsync(
        UploadSessionData sessionData, HttpClient httpClient, CancellationToken cancellationToken)
    {
        try { return await InitializeAsyncCore(sessionData, httpClient, cancellationToken).ConfigureAwait(false); }
        catch (Exception ex) { _logger.Error(ex, "Upload session couldn't be initialized"); throw; }
    }

    static async Task<UploadSession> InitializeAsyncCore(
        UploadSessionData sessionData, HttpClient httpClient, CancellationToken cancellationToken) =>
            await sessionData.UseToRequestUploadSessionAsyncUsing(httpClient, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await FinalizeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <returns>The <c>iid</c> of the media file uploaded to M+.</returns>
    internal async ValueTask<string> FinalizeAsync()
    {
        if (_iid is null)
        {
            var response = await HttpClient.PostAsync(
                $"https://{Host}/content/vcupload/finish",
                new FormUrlEncodedContent(new Dictionary<string, string>() { ["fileid"] = FileId })
                ).ConfigureAwait(false);
            _iid = (string)(await Api.GetJsonIfSuccessfulAsync(response))["iid"]!;

            _logger.Debug("Upload session is successfully finalized");
        }
        else _logger.Warn("Upload session is already finalized");

        return _iid;
    }
}
