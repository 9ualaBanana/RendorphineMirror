using System.Diagnostics;
using System.Management.Automation;

namespace Hardware;

public readonly record struct DiskInfo(
    string? SerialNumber,
    string? Caption,
    MemoryInfo StorageSpace,
    string? FileSystem)
{
    public async static Task<DiskInfo> GetFor(string volumeSerialNumber)
    {
        return (await GetForAll()).SingleOrDefault(disk => disk.SerialNumber == volumeSerialNumber);
    }

    public async static Task<List<DiskInfo>> GetForAll()
    {
        if (OperatingSystem.IsWindows()) return await WinGetForAll();
        if (OperatingSystem.IsLinux()) return await LinuxGetForAll();
        throw new NotImplementedException();
    }

    public async static Task<List<DiskInfo>> WinGetForAll()
    {
        return (await WinQueryDiskInfoForAll())
            .Select(diskInfoQueryResult => GetDiskInfoFrom(diskInfoQueryResult))
            .ToList();
    }

    async static Task<PSDataCollection<PSObject>> WinQueryDiskInfoForAll()
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
        return await powerShell.InvokeAsync();
    }

    static DiskInfo GetDiskInfoFrom(PSObject powerShellQueryResult)
    {
        var volumeSerialNumber = powerShellQueryResult.Properties["VolumeSerialNumber"]?.Value.ToString();
        var caption = powerShellQueryResult.Properties["Caption"]?.Value.ToString();
        var fileSystem = powerShellQueryResult.Properties["FileSystem"]?.Value.ToString();

        var freeStorageSpace = powerShellQueryResult.Properties["FreeSpace"].Value<ulong?>();
        var size = powerShellQueryResult.Properties["Size"].Value<ulong?>();

        double? usedStorageSpace = null;
        if (freeStorageSpace is not null && size is not null)
        {
            usedStorageSpace = ((ulong)size - (ulong)freeStorageSpace).KB().MB().GB();
        }
        var storageSpace = new MemoryInfo(usedStorageSpace, size?.KB().MB().GB());

        return new(volumeSerialNumber, caption, storageSpace, fileSystem);
    }

    async static Task<List<DiskInfo>> LinuxGetForAll()
    {
        return (await LinuxQueryDiskInfoForAll())
            .Select(diskInfoQueryResult => GetDiskInfoFrom(diskInfoQueryResult))
            .ToList();
    }

    async static Task<List<string>> LinuxQueryDiskInfoForAll()
    {
        var startInfo = new ProcessStartInfo("sudo hwinfo")
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            Arguments = "--disk | egrep \"\\[|Unique ID|Capacity\""
        };
        return (await Process.Start(startInfo)!.StandardOutput.ReadToEndAsync())
            .Split("[").ToList();
    }

    static DiskInfo GetDiskInfoFrom(string linuxQueryResult)
    {
        var linuxQueryResults = linuxQueryResult.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var diskSerialNumber = linuxQueryResults[1].Value();
        string? caption = null;
        string? fileSystem = null;

        var size = double.Parse(linuxQueryResults[2].Value(true));
        var storageSpace = new MemoryInfo(default, size);

        return new(diskSerialNumber, caption, storageSpace, fileSystem);
    }
}
