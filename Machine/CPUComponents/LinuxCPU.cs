using System.Runtime.Versioning;

namespace Machine;

[SupportedOSPlatform("linux")]
internal static class LinuxCPU
{
    static (long u1, long t1) PreviousLoadMeasurement = ReadStat();
    static DateTime PreviousLoadMeasurementTime = DateTime.Now;

    static (long, long) ReadStat()
    {
        var line = File.ReadLines("/proc/stat").First();
        var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var u1 = long.Parse(fields[1]) + long.Parse(fields[3]);
        var t1 = u1 + long.Parse(fields[4]);

        return (u1, t1);
    }
    static double MeasureLoad()
    {
        var (u1, t1) = PreviousLoadMeasurement;
        var (u2, t2) = ReadStat();
        var now = DateTime.Now;

        var diff = (now - PreviousLoadMeasurementTime).TotalSeconds;
        var cpuUsage = ((u2 - u1) * 100d / (t2 - t1)) / diff;
        if (diff < 0.05) cpuUsage = 0;

        PreviousLoadMeasurement = (u2, t2);
        PreviousLoadMeasurementTime = now;

        return cpuUsage;
    }

    internal static IReadOnlyList<CPU> Info => [new CPU((uint) (MeasureLoad() * 100))];
}
