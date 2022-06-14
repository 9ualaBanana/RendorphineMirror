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
        using var logicalDrivesSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk");
        using var logicalDrives = logicalDrivesSearcher.Get();

        var container = new Container();
        foreach (var logicalDrive in logicalDrives) container.Add(logicalDrive);
        return container;
    }

    [SupportedOSPlatform("windows")]
    public static long GetFreeSpaceOnDisk(uint diskId)
    {
        var logicalDrivesNamesOnDisk = GetLogicalDrivesNamesOnDisk(diskId);

        long freeSpaceOnDisk = 0;
        foreach (var logicalDrive in Info().Components)
        {
            var typedLogicalDrive = (ManagementBaseObject)logicalDrive;
            var currentLogicalDriveName = typedLogicalDrive["DeviceID"].ToString();
            if (logicalDrivesNamesOnDisk.Any(logicalDriveNameOnDisk => logicalDriveNameOnDisk == currentLogicalDriveName))
            {
                var freeSpaceOnLogicalDriveFromThatDisk = long.Parse(typedLogicalDrive["FreeSpace"].ToString()!);
                freeSpaceOnDisk += freeSpaceOnLogicalDriveFromThatDisk;
            }
        }
        return freeSpaceOnDisk;
    }

    [SupportedOSPlatform("windows")]
    public static IEnumerable<string> LogicalDrivesNamesFromDistinctDisks
    {
        get
        {
            return LogicalDrivesNamesToDisks
                .DistinctBy(logicalDriveNameToDisk => logicalDriveNameToDisk.DiskID)
                .Select(logicalDriveNameToDisk => logicalDriveNameToDisk.LogicalDriveName);
        }
    }

    [SupportedOSPlatform("windows")]
    public static IEnumerable<string> GetLogicalDrivesNamesOnDisk(uint diskId)
    {
        using var disksToPartitionsSearcher = new ManagementObjectSearcher("SELECT Antecedent, Dependent FROM Win32_LogicalDiskToPartition");
        using var disksToPartitions = disksToPartitionsSearcher.Get();

        return LogicalDrivesNamesToDisks
            .Where(logicalDriveNameToDisk => logicalDriveNameToDisk.DiskID == diskId)
            .Select(logicalDriveNameToDisk => logicalDriveNameToDisk.LogicalDriveName);
    }

    [SupportedOSPlatform("windows")]
    static IList<(string LogicalDriveName, uint DiskID)> LogicalDrivesNamesToDisks
    {
        get
        {
            using var disksToPartitionsSearcher = new ManagementObjectSearcher("SELECT Antecedent, Dependent FROM Win32_LogicalDiskToPartition");
            using var disksToPartitions = disksToPartitionsSearcher.Get();

            var logicalDrivesNamesToDisks = new List<(string , uint)>(disksToPartitions.Count);
            foreach (var diskToPartition in disksToPartitions)
            {
                var logicalDriveName = ParseLogicalDriveNameFromDiskToPartition(diskToPartition);
                var diskId = ParseDiskIDFromDiskToPartition(diskToPartition);

                logicalDrivesNamesToDisks.Add((logicalDriveName, diskId));
            }

            return logicalDrivesNamesToDisks;
        }
    }

    [SupportedOSPlatform("windows")]
    static uint ParseDiskIDFromDiskToPartition(ManagementBaseObject diskToPartition)
    {
        return uint.Parse(
            string.Join(
                null,
                diskToPartition["Antecedent"].ToString()!.Split("Disk #")[1].TakeWhile(c => char.IsDigit(c)))
            );
    }

    [SupportedOSPlatform("windows")]
    static string ParseLogicalDriveNameFromDiskToPartition(ManagementBaseObject diskToPartition)
    {
        return string.Join(
            null,
            diskToPartition["Dependent"].ToString()!.SkipWhile(c => c != '"').Skip(1).Take(2));
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
