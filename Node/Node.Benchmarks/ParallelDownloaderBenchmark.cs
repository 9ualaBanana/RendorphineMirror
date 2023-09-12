namespace Node.Benchmarks;

[ShortRunJob]
public class ParallelDownloaderBenchmark
{
    public static ImmutableArray<Uri> Uris { get; } = ImmutableArray.Create(
        //new Uri("http://speedtest.ftp.otenet.gr/files/test100k.db"),
        //new Uri("http://speedtest.ftp.otenet.gr/files/test1Mb.db"),
        new Uri("https://link.testfile.org/70MB")
    //new Uri("https://link.testfile.org/300MB")
    //new Uri("http://ipv4.download.thinkbroadband.com/200MB.zip")
    );

    [ParamsSource(nameof(Uris))]
    public Uri Uri { get; set; } = null!;

    [Benchmark(Baseline = true)]
    public async Task NormalSpeed() => await new HttpClient().GetByteArrayAsync(Uri);

    [Benchmark]
    public async Task ParallelSpeed() => await new ParallelDownloader() { Dirs = new DataDirs("renderfin") }.Download(new HttpClient(), Uri, new MemoryStream(), default);
}
