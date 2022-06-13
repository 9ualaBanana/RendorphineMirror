namespace Benchmark;

public readonly record struct BenchmarkResult(
    long DataSize,
    TimeSpan Time)
{
    public double Rate => DataSize / Time.TotalSeconds;
}
