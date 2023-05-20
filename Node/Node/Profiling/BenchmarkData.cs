namespace Node.Profiling;

public record BenchmarkData(CPUBenchmarkResult CPU, GPUBenchmarkResult GPU, RAMInfo RAM, List<DriveBenchmarkResult> Disks);
