using System.Collections.ObjectModel;
using System.Management.Automation;

namespace Hardware;

public readonly record struct DiskInfo(
    string VolumeSerialNumber,
    string Caption,
    MemoryInfo StorageSpace,
    string FileSystem)
{
    public static DiskInfo GetFor(string volumeSerialNumber)
    {
        return GetForAll().SingleOrDefault(disk => disk.VolumeSerialNumber == volumeSerialNumber);
    }

    public static List<DiskInfo> GetForAll()
    {
        return QueryDiskInfoForAll()
            .Select(diskInfoQueryResult => GetDiskInfoFrom(diskInfoQueryResult))
            .ToList();
    }

    static Collection<PSObject> QueryDiskInfoForAll()
    {
        var powerShell = PowerShell.Create();
        powerShell
            .AddCommand("Get-CIMInstance")
            .AddArgument("Win32_LogicalDisk")
            .AddCommand("Select-Object")
            .AddParameters(new Dictionary<string, List<string>>()
            {
                { "Property", new()
                    {
                        "VolumeSerialNumber",
                        "Caption",
                        "FreeSpace",
                        "Size",
                        "FileSystem"
                    }
                }
            });
        return powerShell.Invoke();
    }

    static DiskInfo GetDiskInfoFrom(PSObject queryResult)
    {
        var volumeSerialNumber = queryResult.Properties["VolumeSerialNumber"].Value.ToString()!;
        var caption = queryResult.Properties["Caption"].Value.ToString()!;
        var fileSystem = queryResult.Properties["FileSystem"].Value.ToString()!;

        var freeStorageSpace = (ulong)queryResult.Properties["FreeSpace"].Value;
        var size = (ulong)queryResult.Properties["Size"].Value;
        var usedStorageSpace = (size - freeStorageSpace).KB().MB().GB();
        var storageSpace = new MemoryInfo(usedStorageSpace, size.KB().MB().GB());

        return new(volumeSerialNumber, caption, storageSpace, fileSystem);
    }
}
