using System.Management.Automation;

namespace Hardware;

public readonly record struct RamInfo(string DeviceLocator, double Capacity, uint MemoryClock)
{
    public async static Task<RamInfo> GetFor(string deviceLocator)
    {
        return (await GetForAll()).SingleOrDefault(ram => ram.DeviceLocator == deviceLocator);
    }

    public async static Task<List<RamInfo>> GetForAll()
    {
        return (await QueryRamInfoForAll())
            .Select(ramInfoQueryResult => GetRamInfoFrom(ramInfoQueryResult))
            .ToList();
    }

    async static Task<PSDataCollection<PSObject>> QueryRamInfoForAll()
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

    static RamInfo GetRamInfoFrom(PSObject queryResult)
    {
        var serialNumber = queryResult.Properties["DeviceLocator"].Value.ToString()!;
        var capacity = ((ulong)queryResult.Properties["Capacity"].Value).KB().MB().GB();
        var memoryClock = (uint)queryResult.Properties["Speed"].Value;
        return new(serialNumber, capacity, memoryClock);
    }
}
