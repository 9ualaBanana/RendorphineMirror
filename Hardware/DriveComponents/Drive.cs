namespace Hardware;

public record Drive(
    uint Id,
    ulong Size,
    ulong FreeSpace,
    IEnumerable<DriveInfo> LogicalDrives)
{
    public static List<Drive> Info
    {
        get
        {
            if (OperatingSystem.IsWindows()) return WindowsDrive.Info;
            //if (OperatingSystem.IsLinux()) return await LinuxGetForAll();
            throw new PlatformNotSupportedException();
        }
    }

    public static IEnumerable<string> LogicalDisksNamesFromDistinctDrives
    {
        get
        {
            if (OperatingSystem.IsWindows()) return WindowsDrive.LogicalDisksNamesFromDistinctDrives;
            throw new PlatformNotSupportedException();
        }
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
