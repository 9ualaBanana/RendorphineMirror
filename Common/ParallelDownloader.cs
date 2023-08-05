using System.Globalization;
using System.Web;

namespace Common;

public static class ParallelDownloader
{
    public static Task Download(HttpClient client, Uri uri, Stream destination, CancellationToken token) => Download(client, uri, destination, 4, token);
    static async Task Download(HttpClient client, Uri uri, Stream destination, int parallelDownloads, CancellationToken token)
    {
        var fileSize = await getFileSize();
        async Task<long> getFileSize()
        {
            using var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri), HttpCompletionOption.ResponseHeadersRead, token);
            return response.Content.Headers.ContentLength.ThrowIfValueNull("Could not get content length");
        }

        using var _ = Directories.TempFiles(parallelDownloads, out var tempfiles, "chunkdownload", HttpUtility.UrlEncode(uri.ToString()));

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
                using var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri) { Headers = { Range = new(range.Start, range.End) } });
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
