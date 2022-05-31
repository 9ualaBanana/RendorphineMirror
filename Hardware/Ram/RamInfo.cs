using System.Diagnostics;
using System.Management.Automation;

namespace Hardware;

public readonly record struct RamInfo(string DeviceLocator, MemoryInfo Memory, uint MemoryClock)
{
    public async static Task<RamInfo> GetFor(string deviceLocator)
    {
        return (await GetForAll()).SingleOrDefault(ram => ram.DeviceLocator == deviceLocator);
    }

    public async static Task<List<RamInfo>> GetForAll()
    {
        if (OperatingSystem.IsWindows()) return await WinGetForAll();
        if (OperatingSystem.IsLinux()) return await LinuxGetForAll();
        throw new NotImplementedException();
    }

    async static Task<List<RamInfo>> WinGetForAll()
    {
        return (await WinQueryRamInfoForAll())
            .Select(ramInfoQueryResult => GetRamInfoFrom(ramInfoQueryResult))
            .ToList();
    }

    async static Task<PSDataCollection<PSObject>> WinQueryRamInfoForAll()
    {
        var powerShell = PowerShell.Create()
            .AddCommand("Get-CIMInstance")
            .AddArgument("Win32_PhysicalMemory")
            .AddCommand("Select-Object")
            .AddParameters(new Dictionary<string, List<string>>()
            {
                { "Property", new()
                    {
                        "DeviceLocator",
                        "Capacity",
                        "Speed"
                    }
                }
            });
        return await powerShell.InvokeAsync();
    }


    async static Task<List<RamInfo>> LinuxGetForAll()
    {
        return new() { GetRamInfoFrom(await UnixQueryRamInfoForAll()) };
    }

    async static Task<string> UnixQueryRamInfoForAll()
    {
        var startInfo = new ProcessStartInfo("lscpu")
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true
        };
        return (await Process.Start(startInfo)!
            .StandardOutput.ReadToEndAsync())
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .First();
    }

    static RamInfo GetRamInfoFrom(PSObject powerShellQueryResult)
    {
        var deviceLocator = powerShellQueryResult.Properties["DeviceLocator"].Value.ToString()!;
        var capacity = ((ulong)powerShellQueryResult.Properties["Capacity"].Value).KB().MB();
        var memoryClock = (uint)powerShellQueryResult.Properties["Speed"].Value;
        return new(deviceLocator, new(default, capacity), memoryClock);
    }

    static RamInfo GetRamInfoFrom(string linuxQueryResult)
    {
        var queryResults = linuxQueryResult.Split();

        var deviceLocator = "All available RAM";
        uint memoryClock = default;

        var used = ulong.Parse(queryResults[2]);
        var total = ulong.Parse(queryResults[1]);
        var memoryInfo = new MemoryInfo(used, total);

        return new(deviceLocator, memoryInfo, memoryClock);
    }
}
