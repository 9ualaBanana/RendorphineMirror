using Benchmark;

namespace Node.P2P;

internal static class PacketsTransporter
{
    internal static async Task<BenchmarkResult> UploadAsync(
        UploadSessionData sessionData,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        httpClient ??= new();

        var uploadResult = new BenchmarkResult(sessionData.File.Length);
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
        }
        return uploadResult;
    }
}
