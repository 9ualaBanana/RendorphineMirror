using System.Diagnostics;
using System.IO.Compression;

namespace Benchmark;

public class ZipBenchmark : IDisposable
{
    readonly Stream _stream;
    readonly Stream _zippedStream;

    public ZipBenchmark(uint streamSize)
    {
        _stream = GenerateTrashDataStream(streamSize);
        _zippedStream = new MemoryStream((int)streamSize);
    }

    static Stream GenerateTrashDataStream(uint streamSize)
    {
        var trashData = new byte[streamSize];
        new Random().NextBytes(trashData);
        return new MemoryStream(trashData);
    }

    public async Task<BenchmarkResult> RunAsync()
    {
        using var zipper = new GZipStream(_zippedStream, CompressionMode.Compress);
        var sw = Stopwatch.StartNew();
        await _stream.CopyToAsync(zipper);
        sw.Stop();

        return new(_stream.Length, sw.Elapsed);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _stream?.Dispose();
        _zippedStream?.Dispose();
    }
}
