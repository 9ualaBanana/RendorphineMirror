using System.ComponentModel;
using System.Management;
using System.Runtime.Versioning;

namespace Hardware;

public static class Disks
{
    public static Container Info()
    {
        if (OperatingSystem.IsWindows()) return WindowsInfo();
        //if (OperatingSystem.IsLinux()) return await LinuxGetForAll();
        throw new PlatformNotSupportedException();
    }

    [SupportedOSPlatform("windows")]
    public static Container WindowsInfo()
    {
        using var diskSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk");
        using var disks = diskSearcher.Get();

        var container = new Container();
        foreach (var disk in disks) container.Add(disk);
        return container;
    }

    //async static Task<List<Disk>> LinuxGetForAll()
    //{
    //    return (await LinuxQueryDiskInfoForAll())
    //        .Select(diskInfoQueryResult => GetDiskInfoFrom(diskInfoQueryResult))
    //        .ToList();
    //}

    //async static Task<List<string>> LinuxQueryDiskInfoForAll()
    //{
    //    var startInfo = new ProcessStartInfo("sudo hwinfo")
    //    {
    //        CreateNoWindow = true,
    //        RedirectStandardOutput = true,
    //        Arguments = "--disk | egrep \"\\[|Unique ID|Capacity\""
    //    };
    //    return (await Process.Start(startInfo)!.StandardOutput.ReadToEndAsync())
    //        .Split("[").ToList();
    //}

    //static Disk GetDiskInfoFrom(string linuxQueryResult)
    //{
    //    var linuxQueryResults = linuxQueryResult.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    //    var diskSerialNumber = linuxQueryResults[1].Value();
    //    string? caption = null;
    //    string? fileSystem = null;

    //    var size = double.Parse(linuxQueryResults[2].Value(true));
    //    var storageSpace = new MemoryInfo(default, size);

    //    return new(diskSerialNumber, caption, storageSpace, fileSystem);
    //}
}
