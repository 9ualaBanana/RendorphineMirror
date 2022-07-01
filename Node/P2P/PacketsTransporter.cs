using Benchmark;

namespace Node.P2P;

internal class PacketsTransporter
{
    internal RequestOptions RequestOptions { get; set; }

    internal PacketsTransporter(RequestOptions? requestOptions = null)
    {
        RequestOptions = requestOptions ?? new();
    }

    internal async Task<BenchmarkResult> UploadAsync(string filePath)
        => await UploadAsync(new FileInfo(filePath));

    internal async Task<BenchmarkResult> UploadAsync(FileInfo file)
    {
        var uploadResult = new BenchmarkResult(file.Length);
        while (true)
        {
            var uploadSession = await UploadSession.InitializeAsync(file, RequestOptions).ConfigureAwait(false);
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
