using System.Net;
using System.Runtime.CompilerServices;

namespace Node.Common;

[AutoRegisteredService(false)]
public class ParallelDownloader
{
    public required DataDirs Dirs { get; init; }
    public required ITaskProgressSetter ProgressSetter { get; init; }
    public required ILogger<ParallelDownloader> Logger { get; init; }
    public required HttpClient HttpClient { get; init; }

    public Task Download(Uri uri, Stream destination, CancellationToken token) => Download(uri, destination, 4, token);
    async Task Download(Uri uri, Stream destination, int parallelDownloads, CancellationToken token)
    {
        using var _logscope = Logger.BeginScope($"Downloading {uri}");

        var fs = await getSizeOrDownload(uri);
        if (fs is null) return;
        var fileSize = fs.Value;

        async Task<long?> getSizeOrDownload(Uri uri)
        {
            using var fileSizeResponse = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri), HttpCompletionOption.ResponseHeadersRead, token);
            Logger.LogInformation("File size HEAD request response returned: " + string.Join(", ", fileSizeResponse.Headers.Select(h => $"{h.Key}: {string.Join(" | ", h.Value)}")));
            Logger.LogInformation("and http " + fileSizeResponse.StatusCode);

            if (fileSizeResponse.StatusCode == HttpStatusCode.Found)
                return await getSizeOrDownload(fileSizeResponse.Headers.Location.ThrowIfNull());

            if (!fileSizeResponse.IsSuccessStatusCode || fileSizeResponse.Content.Headers.ContentLength is null)
            {
                Logger.LogWarning($"Could not fetch file size, downloading normally...");
                fileSizeResponse.EnsureSuccessStatusCode();
                await fileSizeResponse.Content.CopyToAsync(destination, token);

                return null;
            }

            return fileSizeResponse.Content.Headers.ContentLength.Value;
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
            long progress = 0;
            await Task.WhenAll(ranges.Select(async range =>
            {
                using var response = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri) { Headers = { Range = new(range.Start, range.End) } }, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();

                using var fileStream = File.Create(range.ChunkFile);
                var buffer = new byte[1024 * 1024];
                using var stream = await response.Content.ReadAsStreamAsync(token);

                while (true)
                {
                    var length = await stream.ReadAsync(buffer, token);
                    if (length == 0) break;

                    Interlocked.Add(ref progress, length);
                    ProgressSetter.Set((double) progress / fileSize);

                    await fileStream.WriteAsync(buffer.AsMemory(0, length), token);
                }
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


    public async Task DownloadInMemoryAsync(string url, Stream destination, CancellationToken token, int maxThreadCount = 4, long chunkSize = 100L * 1024L * 1024L)
    {
        using var _logscope = Logger.BeginScope($"Downloading {url}");

        var maybeFileSize = await getFileSize();
        async Task<long?> getFileSize()
        {
            using var fileSizeResponse = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url), HttpCompletionOption.ResponseHeadersRead, token);
            if (!fileSizeResponse.IsSuccessStatusCode || fileSizeResponse.Content.Headers.ContentLength is null)
                return null;

            return fileSizeResponse.Content.Headers.ContentLength.Value;
        }

        if (maybeFileSize is null)
        {
            Logger.LogWarning($"Could not fetch file size, downloading normally...");
            using var downloadResponse = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
            downloadResponse.EnsureSuccessStatusCode();
            await downloadResponse.Content.CopyToAsync(destination, token);

            return;
        }

        var fileSize = maybeFileSize.Value;

        var i = 0;
        await foreach (var stream in startDownloading(token))
        {
            Console.WriteLine((i * chunkSize) + "/" + fileSize + " ... " + chunkSize);
            destination.Position = i * chunkSize;
            await stream.CopyToAsync(destination);
            stream.Dispose();

            i++;
        }


        async IAsyncEnumerable<Stream> startDownloading([EnumeratorCancellation] CancellationToken token)
        {
            var semaphore = new PrioritySemaphore(maxThreadCount, maxThreadCount);

            var tasks = new List<Task<Stream>>();
            for (var i = 0L; i < fileSize; i += chunkSize)
            {
                token.ThrowIfCancellationRequested();

                var start = i;
                var end = Math.Min(fileSize, i + chunkSize);

                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync(start);
                    using var _ = new FuncDispose(semaphore.Release);

                    return await downloadChunk(start, end, token);
                }));
            }

            foreach (var task in tasks)
                yield return await task;
        }

        async Task<Stream> downloadChunk(long start, long end, CancellationToken token)
        {
            var response = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url) { Headers = { Range = new(start, end) } }, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync(token);
        }
    }

    record struct DownloadRange(string ChunkFile, long? Start, long? End);
}
