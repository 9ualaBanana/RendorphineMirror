using UnitsNet;

namespace Benchmark;

public record struct BenchmarkResult
{
    public long DataSize { get; set; }
    public TimeSpan Time { get; set; }

    public BenchmarkResult(long dataSize, TimeSpan? time = null)
    {
        DataSize = dataSize;
        Time = time ?? TimeSpan.Zero;
    }

    public double Bps => DataSize / Time.TotalSeconds;
    public double MBps => Information.FromBytes(Bps).Megabytes;
}
