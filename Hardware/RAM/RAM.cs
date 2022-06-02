using System.ComponentModel;
using System.Management;
using System.Runtime.Versioning;

namespace Hardware;

public static class RAM
{
    public static Container Info()
    {
        if (OperatingSystem.IsWindows()) return WindowsInfo();
        //if (OperatingSystem.IsLinux()) return LinuxGetForAll();
        throw new PlatformNotSupportedException();
    }

    [SupportedOSPlatform("windows")]
    static Container WindowsInfo()
    {
        using var ramSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
        using var ramUnits = ramSearcher.Get();

        var container = new Container();
        foreach (var ramUnit in ramUnits) container.Add(ramUnit);
        return container;
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
