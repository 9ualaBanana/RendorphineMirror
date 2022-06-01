using System.Diagnostics;

namespace Hardware;

public readonly record struct GpuInfo(Guid Id, string Name, MemoryInfo Memory, GpuClockInfo GpuClockInfo)
{
    public static List<GpuInfo> GetForAll()
    {
        using var queryResult = Process.Start(GetForAllStartInfo())!;
        var allGpuHardwareIds = queryResult.StandardOutput.ReadToEnd()!;

        return allGpuHardwareIds
            .Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(hardwareId => GetFor(hardwareId))
            .ToList();
    }

    static ProcessStartInfo GetForAllStartInfo()
    {
        var query = "--query-gpu=uuid";
        var format = "--format=csv,noheader,nounits";

        return new ProcessStartInfo("nvidia-smi")
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            Arguments = $"{query} {format}"
        };
    }

    public static GpuInfo GetFor(string hardwareId)
    {
        var guid = GetGuidFromHardwareId(hardwareId);
        return GetGpuInfoFrom(QueryGpuInfoFor(hardwareId), guid);
    }

    static string QueryGpuInfoFor(string hardwareId)
    {
        using var process = Process.Start(GetForStartInfo(hardwareId))!;
        return process.StandardOutput.ReadLine()!;
    }

    static ProcessStartInfo GetForStartInfo(string hardwareId)
    {
        var query = "--query-gpu=" +
                    "uuid," +
                    "name," +
                    "memory.used,memory.total," +
                    "clocks.current.graphics,clocks.max.graphics," +
                    "clocks.current.memory,clocks.max.memory";
        var format = "--format=csv,noheader,nounits";

        return new ProcessStartInfo("nvidia-smi")
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            Arguments = $"{query} -i={hardwareId} {format}"
        };
    }

    static GpuInfo GetGpuInfoFrom(string queryResult, Guid guid)
    {
        var splitQueryResult = queryResult.Split(',', StringSplitOptions.TrimEntries);

        var name = splitQueryResult[1];

        _ = ulong.TryParse(splitQueryResult[2], out var memoryUsed);
        _ = ulong.TryParse(splitQueryResult[3], out var memoryTotal);
        var memoryInfo = new MemoryInfo(memoryUsed, memoryTotal);

        _ = int.TryParse(splitQueryResult[4], out var currentGraphicsClock);
        _ = int.TryParse(splitQueryResult[5], out var maxGraphicsClock);
        _ = int.TryParse(splitQueryResult[6], out var currentMemoryClock);
        _ = int.TryParse(splitQueryResult[7], out var maxMemoryClock);
        var clockInfo = new GpuClockInfo(
            currentGraphicsClock, maxGraphicsClock,
            currentMemoryClock, maxMemoryClock);

        return new(guid, name, memoryInfo, clockInfo);
    }

    static Guid GetGuidFromHardwareId(string prefixedGuid)
    {
        return Guid.Parse(prefixedGuid[4..]);
    }
}
