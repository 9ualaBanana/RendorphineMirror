using Benchmark;

namespace Node.P2P;

internal class PacketsTransporter
{
    internal RequestOptions RequestOptions { get; set; }

    internal PacketsTransporter(RequestOptions? requestOptions = null)
    {
        RequestOptions = requestOptions ?? new();
    }

    internal async Task<BenchmarkResult> UploadAsync(UploadSessionData sessionData)
    {
        var uploadResult = new BenchmarkResult(sessionData.File.Length);
        while (true)
        {
            var uploadSession = await UploadSession.InitializeAsync(sessionData, RequestOptions).ConfigureAwait(false);
            using var packetsUploader = new PacketsUploader(uploadSession);
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
