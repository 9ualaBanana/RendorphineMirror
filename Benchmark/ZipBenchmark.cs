using System.Diagnostics;
using System.IO.Compression;

namespace Benchmark;

public class ZipBenchmark
{
    readonly int _dataSize;
    readonly DirectoryInfo _directoryToZip;

    public ZipBenchmark(int dataSize)
    {
        _dataSize = dataSize;
        _directoryToZip = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        using var file = File.Create(Path.Combine(_directoryToZip.FullName, Path.GetRandomFileName()));
        FillFileWithTrashData(file, dataSize);
    }

    static void FillFileWithTrashData(FileStream file, int dataSize)
    {
        var random = new Random();
        for (int i = 0; i < dataSize; i++)
            file.WriteByte((byte)random.Next(byte.MaxValue));
    }

    public async Task<BenchmarkResult> RunAsync()
    {
        var sw = Stopwatch.StartNew();
        await _directoryToZip.DeleteAfterAsync(ZipAsync);
        sw.Stop();

        return new(_dataSize, sw.Elapsed);
    }

    static async Task ZipAsync(string directoryPath)
    {
        void zipping() => ZipFile.CreateFromDirectory(directoryPath, Path.ChangeExtension(directoryPath, ".zip"));
        await Task.Run(zipping);
    }
}
