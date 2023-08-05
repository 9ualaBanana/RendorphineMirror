namespace Node.Profiling;

public record CPUBenchmarkResult(double Rating, CPUBenchmarkPRatings PRatings)
{
    /// <summary> CPU load, 0-1 </summary>
    public double Load { get; set; }
}
public record CPUBenchmarkPRatings(double FFMpegRating);