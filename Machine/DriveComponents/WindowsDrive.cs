using NLog;
using System.Management;
using System.Runtime.Versioning;

namespace Machine;

[SupportedOSPlatform("Windows")]
internal static class WindowsDrive
{
    static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    internal static List<Drive> Info
    {
        get
        {
            var logicalDisksNamesToDrivesMap = LogicalDisksNamesToDriveIdsMap;
            var drivesIds = logicalDisksNamesToDrivesMap
                .Select(its => its.DriveId)
                .Distinct();

            var drives = new List<Drive>();
            foreach (var driveId in drivesIds)
            {
                drives.Add(
                    new(
                        driveId,
                        GetTotalSpaceOnDrive(driveId),
                        GetFreeSpaceOnDrive(driveId),
                        GetLogicalDisksOnDrive(driveId))
                    );
            }
            return drives;
        }
    }

    internal static IEnumerable<string> LogicalDisksNamesFromDistinctDrives
    {
        get
        {
            return LogicalDisksNamesToDriveIdsMap
                .DistinctBy(its => its.DriveId)
                .Select(its => its.LogicalDiskName);
        }
    }

    static ulong GetTotalSpaceOnDrive(uint driveId)
    {
        var logicalDiskOnDrive = GetLogicalDisksOnDrive(driveId);

        ulong totalSpace = 0;
        foreach (var logicalDisk in DriveInfo.GetDrives())
        {
            if (logicalDiskOnDrive.Any(its => its.Name == logicalDisk.Name))
            {
                totalSpace += logicalDisk.Size(DiskSpace.Total);
            }
        }
        return totalSpace;
    }

    static ulong GetFreeSpaceOnDrive(uint driveId)
    {
        var logicalDisksOnDrive = GetLogicalDisksOnDrive(driveId);

        ulong freeSpaceOnDrive = 0;
        foreach (var logicalDisk in DriveInfo.GetDrives())
        {
            if (logicalDisksOnDrive.Any(its => its.Name == logicalDisk.Name))
            {
                freeSpaceOnDrive += logicalDisk.Size(DiskSpace.Free);
            }
        }
        return freeSpaceOnDrive;
    }

    static IEnumerable<DriveInfo> GetLogicalDisksOnDrive(uint driveId)
    {
        return LogicalDisksNamesToDriveIdsMap
            .Where(its => its.DriveId == driveId)
            .Select(its => new DriveInfo(its.LogicalDiskName));
    }

    static IList<(string LogicalDiskName, uint DriveId)>? _logicalDisksNamesToDriveIdsMap;
    static IList<(string LogicalDiskName, uint DriveId)> LogicalDisksNamesToDriveIdsMap
    {
        get
        {
            if (_logicalDisksNamesToDriveIdsMap is not null) return _logicalDisksNamesToDriveIdsMap;

            using var drivesToPartitionsSearcher = new ManagementObjectSearcher("SELECT Antecedent, Dependent FROM Win32_LogicalDiskToPartition");
            using var drivesToPartitions = drivesToPartitionsSearcher.Get();

            _logicalDisksNamesToDriveIdsMap = new List<(string, uint)>(drivesToPartitions.Count);
            foreach (var driveToPartition in drivesToPartitions)
            {
                var logicalDiskName = ParseLogicalDiskName(driveToPartition);
                var driveId = ParseDriveId(driveToPartition);

                _logicalDisksNamesToDriveIdsMap.Add((logicalDiskName, driveId));
            }

            return _logicalDisksNamesToDriveIdsMap!;
        }
    }

    static uint ParseDriveId(ManagementBaseObject driveToPartition)
    {
        return uint.Parse(
            string.Join(
                null,
                driveToPartition["Antecedent"].ToString()!.Split("Disk #")[1].TakeWhile(c => char.IsDigit(c)))
            );
    }

    static string ParseLogicalDiskName(ManagementBaseObject driveToPartition)
    {
        return string.Join(
            null,
            driveToPartition["Dependent"].ToString()!.SkipWhile(c => c != '"').Skip(1).Take(2));
    }


    static ulong Size(this DriveInfo logicalDisk, DiskSpace diskSpace)
    {
        try
        {
            return diskSpace switch
            {
                DiskSpace.Total => (ulong)logicalDisk.TotalSize,
                DiskSpace.Free => (ulong)logicalDisk.AvailableFreeSpace,
                _ => throw new NotImplementedException()
            };
        }
        catch (IOException ex)
        {
            _logger.Error(ex, "{TotalSize} of {LogicalDisk} logical disk couldn't be read.", nameof(DriveInfo.TotalSize), logicalDisk.Name);
            return 0;
        }
    }
}

enum DiskSpace { Total, Free }
