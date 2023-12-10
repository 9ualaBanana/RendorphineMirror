namespace Node.Common;

[AutoRegisteredService(false)]
public class ParallelDownloader
{
    public required HttpClient HttpClient { get; init; }
    public required DataDirs Dirs { get; init; }
    public required ILogger<ParallelDownloader> Logger { get; init; }

    public Task Download(Uri uri, Stream destination, CancellationToken token) => Download(uri, destination, 4, token);
    async Task Download(Uri uri, Stream destination, int parallelDownloads, CancellationToken token)
    {
        using var _logscope = Logger.BeginScope($"Downloading {uri}");

        long fileSize;
        {
            using var fileSizeResponse = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri), HttpCompletionOption.ResponseHeadersRead, token);
            if (!fileSizeResponse.IsSuccessStatusCode || fileSizeResponse.Content.Headers.ContentLength is null)
            {
                Logger.LogWarning($"Could not fetch file size, downloading normally...");
                using var downloadResponse = await HttpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);
                downloadResponse.EnsureSuccessStatusCode();
                await downloadResponse.Content.CopyToAsync(destination, token);

                return;
            }

            fileSize = fileSizeResponse.Content.Headers.ContentLength.Value;
        }

        Logger.LogTrace($"File size: {fileSize / 1024f / 1024f} MB");
        using var _ = Directories.DisposeDelete(Dirs.TempFiles(parallelDownloads, $"chunkdownload{Guid.NewGuid()}"), out var tempfiles);

        var ranges = calculateRanges();
        IReadOnlyList<DownloadRange> calculateRanges()
        {
            var ranges = new List<DownloadRange>();
            for (int chunk = 0; chunk < parallelDownloads - 1; chunk++)
            {
                var range = new DownloadRange(
                    tempfiles[chunk],
                    chunk * (fileSize / parallelDownloads),
                    ((chunk + 1) * (fileSize / parallelDownloads)) - 1
                );
                ranges.Add(range);
            }

            ranges.Add(new DownloadRange(
                tempfiles[^1],
                ranges.Any() ? ranges.Last().End + 1 : 0,
                null
            ));

            return ranges;
        }

        await download();
        async Task download()
        {
            await Task.WhenAll(ranges.Select(async range =>
            {
                using var response = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri) { Headers = { Range = new(range.Start, range.End) } }, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();

                using var fileStream = File.Create(range.ChunkFile);
                await response.Content.CopyToAsync(fileStream, token);
            }));
        }

        await output();
        async Task output()
        {
            foreach (var range in ranges)
            {
                using var stream = File.OpenRead(range.ChunkFile);
                await stream.CopyToAsync(destination, token);
            }
        }

        await destination.FlushAsync(token);
    }


    record struct DownloadRange(string ChunkFile, long? Start, long? End);
}
