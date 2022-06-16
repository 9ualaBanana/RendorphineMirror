using System.Text.Json;

namespace Node.Profiler;

public class NodeProfile
{
    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    public string SessionId { get; init; } = null!;
    public string Info { get; init; } = null!;
}

public class Info
{
    public string IP { get; init; } = null!;
    public int Port { get; init; }
    public string Nickname { get; init; } = null!;
    public IDictionary<string, int> AllowedInputs { get; init; } = null!;
    public IDictionary<string, int> AllowedOutputs { get; init; } = null!;
    public IDictionary<TaskType, int> AllowedTypes { get; init; } = null!;
    public string? Hardware { get; init; }
}

public class BenchmarkResults
{
    public CPUBenchmarkResults CPU { get; init; } = null!;
    public GPUBenchmarkResults GPU { get; init; } = null!;
    public RAMBenchmarkResults RAM { get; init; } = null!;
    public DrivesBenchmarkResults[] Disks { get; init; } = null!;
}

public class CPUBenchmarkResults
{
    public double Rating { get; init; }
    public double FFmpegRating { get; init; }
    public int Load { get; init; }
}

public class GPUBenchmarkResults
{
    public double Rating { get; init; }
    public double FFmpegRating { get; init; }
    public int Load { get; init; }
}

public class RAMBenchmarkResults
{
    public ulong Total { get; init; }
    public ulong Free { get; init; }
}

public class DrivesBenchmarkResults
{
    public ulong FreeSpace { get; init; }
    public double WriteSpeed { get; init; }
}
