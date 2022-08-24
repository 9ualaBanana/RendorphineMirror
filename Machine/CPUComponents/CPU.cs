namespace Machine;

public record CPU(
    string Name,
    uint CoreCount,
    uint ThreadCount,
    ulong CurrentClockSpeed,
    ulong MaxClockSpeed,
    uint LoadPercentage)
{
    public static List<CPU> Info
    {
        get
        {
            if (OperatingSystem.IsWindows()) return WindowsCPU.Info;
            //if (OperatingSystem.IsLinux()) return await LinuxGetForAll();
            throw new PlatformNotSupportedException();
        }
    }

    //async static Task<List<CpuInfo>> LinuxGetForAll()
    //{
    //    return (await LinuxQueryCpuInfoForAll())
    //        .Select(cpuInfoQueryResult => GetCpuInfoFrom(cpuInfoQueryResult))
    //        .ToList();
    //}

    //async static Task<List<string>> LinuxQueryCpuInfoForAll()
    //{
    //    var startInfo = new ProcessStartInfo("sudo dmidecode")
    //    {
    //        CreateNoWindow = true,
    //        RedirectStandardOutput = true,
    //        Arguments = "-t processor | egrep \"Handle|ID|Version|Max Speed|Current Speed|Core Count|Thread Count\""
    //    };
    //    return (await Process.Start(startInfo)!.StandardOutput.ReadToEndAsync())
    //        .Split("Handle").ToList();  // Likely makes a split per core rather than whole CPU.
    //}

    //static CpuInfo GetCpuInfoFrom(string linuxQueryResult)
    //{
    //    var linuxQueryResults = linuxQueryResult.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    //    string id = linuxQueryResults[1].Value();
    //    var name = linuxQueryResults[2].Value();

    //    uint currentClockSpeed = 0, maxClockSpeed = 0;
    //    if (float.TryParse(linuxQueryResults[4].Value(true), out var rawCurrentClockSpeed))
    //    {
    //        currentClockSpeed = (uint)Math.Round(rawCurrentClockSpeed);
    //    }
    //    if (float.TryParse(linuxQueryResults[3].Value(true), out var rawMaxClockSpeed))
    //    {
    //        maxClockSpeed = (uint)Math.Round(rawMaxClockSpeed);
    //    }
    //    var clockInfo = new CpuClockInfo(currentClockSpeed, maxClockSpeed);

    //    uint.TryParse(linuxQueryResults[8].Value(), out var coreCount);
    //    uint.TryParse(linuxQueryResults[6].Value(), out var threadCount);
    //    ushort loadPercentage = default;

    //    return new(id, name, coreCount, threadCount, clockInfo, loadPercentage);
    //}
}
