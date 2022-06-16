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
    public string AllowedInputs { get; init; } = null!;
    public string AllowedOutputs { get; init; } = null!;
    public string AllowedTypes { get; init; } = null!;
    public string? Hardware { get; init; }
}

public class BenchmarkResults
{
    public CPUPayload CPU { get; init; } = null!;
    public GPUPayload GPU { get; init; } = null!;
    public RAMPayload RAM { get; init; } = null!;
    public DrivesPayload[] Disks { get; init; } = null!;
}

public class CPUPayload
{
    public double Rating { get; init; }
    public double FFmpegRating { get; init; }
    public int Load { get; init; }
}

public class GPUPayload
{
    public double Rating { get; init; }
    public double FFmpegRating { get; init; }
    public int Load { get; init; }
}

public class RAMPayload
{
    public ulong Total { get; init; }
    public ulong Free { get; init; }
}

public class DrivesPayload
{
    public ulong FreeSpace { get; init; }
    public double WriteSpeed { get; init; }
}
