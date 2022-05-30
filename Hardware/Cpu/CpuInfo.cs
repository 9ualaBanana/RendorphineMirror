using System.Management.Automation;

namespace Hardware;

public readonly record struct CpuInfo(
    string Id,
    string Name,
    uint CoreCount,
    uint ThreadCount,
    CpuClockInfo CpuClockInfo,
    ushort LoadPercentage)
{
    public async static Task<CpuInfo> GetFor(string hardwareId)
    {
        return (await GetForAll()).SingleOrDefault(cpu => cpu.Id == hardwareId);
    }

    public async static Task<List<CpuInfo>> GetForAll()
    {
        return (await QueryCpuInfoForAll())
            .Select(cpuInfoQueryResult => GetCpuInfoFrom(cpuInfoQueryResult))
            .ToList();
    }

    async static Task<PSDataCollection<PSObject>> QueryCpuInfoForAll()
    {
        var powerShell = PowerShell.Create();
        powerShell
            .AddCommand("Get-CIMInstance")
            .AddArgument("Win32_Processor")
            .AddCommand("Select-Object")
            .AddParameters(new Dictionary<string, List<string>>()
            {
                { "Property", new()
                    {
                        "ProcessorId",
                        "Name",
                        "CurrentClockSpeed",
                        "MaxClockSpeed",
                        "NumberOfCores",
                        "ThreadCount",
                        "LoadPercentage"
                    }
                }
            });
        return await powerShell.InvokeAsync();
    }

    static CpuInfo GetCpuInfoFrom(PSObject queryResult)
    {
        var id = queryResult.Properties["ProcessorId"].Value.ToString()!;
        var name = queryResult.Properties["Name"].Value.ToString()!;

        var currentClockSpeed = (uint)queryResult.Properties["CurrentClockSpeed"].Value;
        var maxClockSpeed = (uint)queryResult.Properties["MaxClockSpeed"].Value;
        var clockInfo = new CpuClockInfo(currentClockSpeed, maxClockSpeed);

        var coreCount = (uint)queryResult.Properties["NumberOfCores"].Value;
        var threadCount = (uint)queryResult.Properties["ThreadCount"].Value;
        var loadPercentage = (ushort)queryResult.Properties["LoadPercentage"].Value;

        return new(id, name, coreCount, threadCount, clockInfo, loadPercentage);
    }
}
