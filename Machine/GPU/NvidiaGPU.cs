using System.Diagnostics;

namespace Machine;

internal class NvidiaGPU
{
    internal static List<GPU> Info
    {
        get
        {
            using var allGpuUuidsQueryResult = Process.Start(AllGpuUuidsQuery)!;
            var allGpuUuids = GetUuidsAsStrings(allGpuUuidsQueryResult);

            return allGpuUuids.Select(gpuUuid => GetInfoFor(gpuUuid)).ToList();
        }
    }

    static ProcessStartInfo AllGpuUuidsQuery
    {
        get
        {
            var query = "--query-gpu=uuid";
            var format = "--format=csv,noheader,nounits";

            return new("nvidia-smi", $"{query} {format}")
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            };
        }
    }

    static IEnumerable<string> GetUuidsAsStrings(Process queryResult)
    {
        var allGpuUuidsUnparsed = queryResult.StandardOutput.ReadToEnd()!;

        return allGpuUuidsUnparsed
            .Split(Environment.NewLine.ToCharArray(), StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    static GPU GetInfoFor(string gpuUuid)
    {
        using var queryResult = Process.Start(GetQueryForGettingInfoFor(gpuUuid))!;
        var guid = ParseUuid(gpuUuid);

        return BuildGpuInfo(queryResult, guid);
    }

    static ProcessStartInfo GetQueryForGettingInfoFor(string uuid)
    {
        var query = "--query-gpu=" +
                    "uuid," +
                    "name," +
                    "memory.used,memory.total," +
                    "clocks.current.graphics,clocks.max.graphics," +
                    "clocks.current.memory,clocks.max.memory";
        var format = "--format=csv,noheader,nounits";

        return new("nvidia-smi", $"{query} -i={uuid} {format}")
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        };
    }

    static GPU BuildGpuInfo(Process queryResult, Guid uuid)
    {
        var splitQueryResult = queryResult.StandardOutput.ReadLine()!
            .Split(',', StringSplitOptions.TrimEntries);

        _ = ulong.TryParse(splitQueryResult[2], out var usedMemory);
        _ = ulong.TryParse(splitQueryResult[3], out var totalMemory);
        _ = ulong.TryParse(splitQueryResult[4], out var currentCoreClock);
        _ = ulong.TryParse(splitQueryResult[5], out var maxCoreClock);
        _ = ulong.TryParse(splitQueryResult[6], out var currentMemoryClock);
        _ = ulong.TryParse(splitQueryResult[7], out var maxMemoryClock);

        return new(
            UUID: uuid,
            Name: splitQueryResult[1],
            UsedMemory: usedMemory,
            TotalMemory: totalMemory,
            CurrentCoreClock: currentCoreClock,
            MaxCoreClock: maxCoreClock,
            CurrentMemoryClock: currentMemoryClock,
            MaxMemoryClock: maxMemoryClock
        );
    }

    static Guid ParseUuid(string prefixedUuid)
    {
        return Guid.Parse(prefixedUuid[4..]);
    }
}
