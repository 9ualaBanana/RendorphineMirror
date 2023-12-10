using Node.Common;

namespace Node.Tests;

[TestFixture]
public class ParallelDownloaderTests
{
    [Test]
    public async Task TestDownloadExactBytes()
    {
        var uri = new Uri("https://izorofficial.ru/img/svg/logo.svg");

        var normalDownload = await new HttpClient().GetByteArrayAsync(uri);

        var chunkedDownload = new MemoryStream();
        await new ParallelDownloader() { Dirs = new DataDirs("renderfin"), HttpClient = new(), Logger = new NLog.Extensions.Logging.NLogLoggerFactory().CreateLogger<ParallelDownloader>() }.Download(uri, chunkedDownload, default);
        chunkedDownload.Position = 0;

        normalDownload.Should().BeEquivalentTo(chunkedDownload.ToArray());
    }
}
