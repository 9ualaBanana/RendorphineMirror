using System.Management;
using System.Runtime.Versioning;

namespace Machine;

[SupportedOSPlatform("windows")]
internal static class WindowsCPU
{
    internal static List<CPU> Info
    {
        get
        {
            using ManagementObjectSearcher cpuSearcher = new("SELECT * FROM Win32_Processor");
            using ManagementObjectCollection cpuMbos = cpuSearcher.Get();

            var cpuUnits = new List<CPU>(cpuMbos.Count);
            foreach (var cpuMbo in cpuMbos)
                cpuUnits.Add(ToCPU(cpuMbo));
            return cpuUnits;
        }
    }

    internal static CPU ToCPU(ManagementBaseObject mbo)
    {
        string name = mbo["Name"]?.ToString() ?? string.Empty;
        _ = uint.TryParse(mbo["NumberOfCores"]?.ToString(), out var coreCount);
        _ = uint.TryParse(mbo["ThreadCount"]?.ToString(), out var threadCount);
        _ = ulong.TryParse(mbo["CurrentClockSpeed"]?.ToString(), out var currentClockSpeed);
        _ = ulong.TryParse(mbo["MaxClockSpeed"]?.ToString(), out var maxClockSpeed);
        _ = uint.TryParse(mbo["LoadPercentage"]?.ToString(), out var loadPercentage);

        return new(name, coreCount, threadCount, currentClockSpeed, maxClockSpeed, loadPercentage);
    }


    public record CPU(string Name, uint CoreCount, uint ThreadCount, ulong CurrentClockSpeed, ulong MaxClockSpeed, uint LoadPercentage) : Machine.CPU(LoadPercentage);
}
