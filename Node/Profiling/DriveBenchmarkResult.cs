namespace Node.Profiling;

public record DriveBenchmarkResult(uint Id, double WriteSpeed)
{
    public ulong FreeSpace { get; set; }
}