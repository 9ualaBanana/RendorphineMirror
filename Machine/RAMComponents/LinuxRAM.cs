using System.Management;
using System.Runtime.Versioning;

namespace Machine;

[SupportedOSPlatform("linux")]
internal static class LinuxRAM
{
    internal static IReadOnlyList<RAM> Info => [new RAM(GetTotal(), GetFree())];

    static ulong GetTotal()
    {
        var meminfo = File.ReadAllText("/proc/meminfo");
        var freeRamKB = ulong.Parse(meminfo.Split('\n')
            .First(line => line.StartsWith("MemTotal:"))
            .Split(':')[1].Trim().Replace(" kB", ""));

        return freeRamKB * 1024;
    }
    static ulong GetFree()
    {
        var meminfo = File.ReadAllText("/proc/meminfo");
        var freeRamKB = ulong.Parse(meminfo.Split('\n')
            .First(line => line.StartsWith("MemAvailable:"))
            .Split(':')[1].Trim().Replace(" kB", ""));

        return freeRamKB * 1024;
    }
}
