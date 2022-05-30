using System.Collections.ObjectModel;
using System.Management.Automation;

namespace Hardware;

public readonly record struct RamInfo(string SerialNumber, double Capacity, uint MemoryClock)
{
    public static RamInfo GetFor(string serialNumber)
    {
        return GetForAll().SingleOrDefault(ram => ram.SerialNumber == serialNumber);
    }

    public static ReadOnlyCollection<RamInfo> GetForAll()
    {
        return QueryRamInfoForAll()
            .Select(ramInfoQueryResult => GetRamInfoFrom(ramInfoQueryResult))
            .ToList().AsReadOnly();
    }

    static Collection<PSObject> QueryRamInfoForAll()
    {
        var powerShell = PowerShell.Create()
            .AddCommand("Get-CIMInstance")
            .AddArgument("Win32_PhysicalMemory")
            .AddCommand("Select-Object")
            .AddParameters(new Dictionary<string, List<string>>()
            {
                { "Property", new()
                    {
                        "SerialNumber",
                        "Capacity",
                        "Speed"
                    }
                }
            });
        return powerShell.Invoke();
    }

    static RamInfo GetRamInfoFrom(PSObject queryResult)
    {
        var serialNumber = queryResult.Properties["SerialNumber"].Value.ToString()!;
        var capacity = ((ulong)queryResult.Properties["Capacity"].Value).KB().MB().GB();
        var memoryClock = (uint)queryResult.Properties["Speed"].Value;
        return new(serialNumber, capacity, memoryClock);
    }
}
