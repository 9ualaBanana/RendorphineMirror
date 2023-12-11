using Node.Common;

namespace Node.Tests;

[TestFixture]
public class ParallelDownloaderTests
{
    [Test]
    public async Task TestDownloadExactBytes()
    {
        static async Task test(string url)
        {
            var normalDownload = await new HttpClient().GetByteArrayAsync(new Uri(url));

            var chunkedDownload = new MemoryStream();
            await new ParallelDownloader() { Dirs = new DataDirs("renderfin"), HttpClient = new(), Logger = new NLog.Extensions.Logging.NLogLoggerFactory().CreateLogger<ParallelDownloader>() }.Download(new Uri(url), chunkedDownload, default);
            chunkedDownload.Position = 0;

            var inmemChunkedDownload = new MemoryStream();
            await new ParallelDownloader() { Dirs = new DataDirs("renderfin"), HttpClient = new(), Logger = new NLog.Extensions.Logging.NLogLoggerFactory().CreateLogger<ParallelDownloader>() }.DownloadInMemoryAsync(url, inmemChunkedDownload, default);
            inmemChunkedDownload.Position = 0;


            normalDownload.Should().BeEquivalentTo(chunkedDownload.ToArray());
            normalDownload.Should().BeEquivalentTo(inmemChunkedDownload.ToArray());
        }


        await test("https://izorofficial.ru/img/svg/logo.svg");
        await test("https://singapore.downloadtestfile.com/50MB.zip");
    }
}
