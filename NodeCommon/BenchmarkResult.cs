using UnitsNet;

namespace NodeCommon;

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
    public double MBps => (double) Information.FromBytes(Bps).Megabytes;
}
