namespace Node.Profiling;

public record DriveBenchmarkResult(double WriteSpeed)
{
    public ulong FreeSpace { get; set; }
}