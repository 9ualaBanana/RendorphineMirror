using System.Diagnostics;

namespace Hardware;

public static class GPU
{
    public static IEnumerable<Dictionary<string, object>> Info()
    {
        try
        {
            return NvidiaInfo();
        }
        catch
        {
            return new List<Dictionary<string, object>>();
        }
    }

    static IEnumerable<Dictionary<string, object>> NvidiaInfo()
    {
        using var queryResult = Process.Start(GetForAllStartInfo())!;
        var allGpuHardwareIds = queryResult.StandardOutput.ReadToEnd()!;

        return allGpuHardwareIds
            .Split(Environment.NewLine.ToCharArray(), StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(hardwareId => GetFor(hardwareId));
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

    public static Dictionary<string, object> GetFor(string hardwareId)
    {
        var guid = GetGuidFromHardwareId(hardwareId);
        return GetGPUInfoFrom(QueryGPUInfoFor(hardwareId), guid);
    }

    static string QueryGPUInfoFor(string hardwareId)
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

    static Dictionary<string, object> GetGPUInfoFrom(string queryResult, Guid guid)
    {
        var splitQueryResult = queryResult.Split(',', StringSplitOptions.TrimEntries);

        var gpuInfo = new Dictionary<string, object>
        {
            { "Name", splitQueryResult[1] },
            { "UsedMemory", splitQueryResult[2] },
            { "TotalMemory", splitQueryResult[3] },
            { "CurrentCoreClock", splitQueryResult[4] },
            { "MaxCoreClock", splitQueryResult[5] },
            { "CurrentMemoryClock", splitQueryResult[6] },
            { "MaxMemoryClock", splitQueryResult[7] }
        };

        return gpuInfo;
    }

    static Guid GetGuidFromHardwareId(string prefixedGuid)
    {
        return Guid.Parse(prefixedGuid[4..]);
    }
}
