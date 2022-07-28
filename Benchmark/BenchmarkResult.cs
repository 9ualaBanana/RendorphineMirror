using UnitsNet;

namespace Benchmark;

public record struct BenchmarkResult
{
    public long DataSize { get; set; }
    public TimeSpan Time { get; set; }

    public BenchmarkResult(long dataSize, TimeSpan time = default)
    {
        DataSize = dataSize;
        Time = time;
    }

    public double Bps => DataSize / Time.TotalSeconds;
    public double MBps => Information.FromBytes(Bps).Megabytes;
}
