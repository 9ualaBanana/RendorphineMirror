namespace Node.Profiling;

public record GPUBenchmarkResult(double Rating, GPUBenchmarkPRatings PRatings)
{
    /// <summary> GPU load, 0-1 </summary>
    public double Load { get; set; }
}
public record GPUBenchmarkPRatings(double FFMpegRating);
