using System.Diagnostics;

namespace Hardware;

public readonly record struct GpuInfo(Guid Id, string Name, MemoryInfo Memory, GpuClockInfo GpuClockInfo)
{
    public static List<GpuInfo> GetForAll()
    {
        using var queryResult = Process.Start(GetForAllStartInfo())!;
        var allGpuHardwareIds = queryResult.StandardOutput.ReadToEnd()!;

        return allGpuHardwareIds
            .Split(Environment.NewLine.ToCharArray(), StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
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

    static string QueryGpuInfoFor(string hardwareId)
    {
        using var process = Process.Start(GetForStartInfo(hardwareId))!;
        return process.StandardOutput.ReadLine()!;
    }

    static GpuInfo GetGpuInfoFrom(string queryResult, Guid guid)
    {
        var splitQueryResult = queryResult.Split(',', StringSplitOptions.TrimEntries);

        var name = splitQueryResult[1];

        var memoryUsed = ulong.Parse(splitQueryResult[2]);
        var memoryTotal = ulong.Parse(splitQueryResult[3]);
        var memoryInfo = new MemoryInfo(memoryUsed, memoryTotal);

        var currentGraphicsClock = int.Parse(splitQueryResult[4]);
        var maxGraphicsClock = int.Parse(splitQueryResult[5]);
        var currentMemoryClock = int.Parse(splitQueryResult[6]);
        var maxMemoryClock = int.Parse(splitQueryResult[7]);
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
