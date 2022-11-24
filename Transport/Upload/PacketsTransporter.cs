using NodeCommon;
using NLog;

namespace Transport.Upload;

public static class PacketsTransporter
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    public static async Task<BenchmarkResult> UploadAsync(
        UploadSessionData sessionData,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        httpClient ??= new();

        var uploadResult = new BenchmarkResult(sessionData.File.Length);
        _logger.Info("Starting upload...");
        while (true)
        {
            var uploadSession = await UploadSession.InitializeAsync(
                sessionData, httpClient, cancellationToken).ConfigureAwait(false);
            using var packetsUploader = new PacketsUploader(uploadSession, httpClient, cancellationToken);
            uploadResult.Time += (await packetsUploader.UploadAsync().ConfigureAwait(false)).Time;
            if (await uploadSession.EnsureAllBytesUploadedAsync().ConfigureAwait(false))
            {
                await uploadSession.FinalizeAsync().ConfigureAwait(false);
                break;
            }
            _logger.Debug("Restarting upload");
        }
        _logger.Info("Upload is complete");
        return uploadResult;
    }
}
