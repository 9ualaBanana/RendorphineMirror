namespace Machine;

public record RAM(ulong Capacity, ulong FreeMemory)
{
    public static IReadOnlyList<RAM> Info
    {
        get
        {
            if (OperatingSystem.IsWindows()) return WindowsRAM.Info;
            if (OperatingSystem.IsLinux()) return LinuxRAM.Info;
            throw new PlatformNotSupportedException();
        }
    }

    //async static Task<List<RAM>> LinuxGetForAll()
    //{
    //    return new() { GetRamInfoFrom(await UnixQueryRamInfoForAll()) };
    //}

    //async static Task<string> UnixQueryRamInfoForAll()
    //{
    //    var startInfo = new ProcessStartInfo("lscpu")
    //    {
    //        CreateNoWindow = true,
    //        RedirectStandardOutput = true
    //    };
    //    return (await Process.Start(startInfo)!
    //        .StandardOutput.ReadToEndAsync())
    //        .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    //        .First();
    //}

    //static RAM GetRamInfoFrom(string linuxQueryResult)
    //{
    //    var queryResults = linuxQueryResult.Split();

    //    var deviceLocator = "All available RAM";
    //    uint memoryClock = default;

    //    var used = ulong.Parse(queryResults[2]);
    //    var total = ulong.Parse(queryResults[1]);
    //    var memoryInfo = new MemoryInfo(used, total);

    //    return new(deviceLocator, memoryInfo, memoryClock);
    //}
}
