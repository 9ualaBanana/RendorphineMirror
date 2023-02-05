using NLog;

namespace Transport.Upload;

public static class PacketsTransporter
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    /// <returns>The <c>iid</c> of the media file uploaded to M+.</returns>
    public static async Task<string> UploadAsync(
        UploadSessionData sessionData,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        httpClient ??= new();

        _logger.Info("Starting upload...");
        while (true)
        {
            var uploadSession = await UploadSession.InitializeAsync(
                sessionData, httpClient, cancellationToken).ConfigureAwait(false);
            using var packetsUploader = new PacketsUploader(uploadSession, httpClient, cancellationToken);
            await packetsUploader.UploadAsync().ConfigureAwait(false);
            if (await uploadSession.EnsureAllBytesUploadedAsync().ConfigureAwait(false))
            {
                _logger.Info("Upload is complete");
                return await uploadSession.FinalizeAsync().ConfigureAwait(false);
            }
            else _logger.Debug("Restarting upload...");
        }
    }
}
