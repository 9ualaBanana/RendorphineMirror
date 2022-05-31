using System.Diagnostics;
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
        if (OperatingSystem.IsWindows()) return await WinGetForAll();
        if (OperatingSystem.IsLinux()) return await LinuxGetForAll();
        throw new NotImplementedException();
    }

    async static Task<List<CpuInfo>> WinGetForAll()
    {
        return (await WinQueryCpuInfoForAll())
            .Select(cpuInfoQueryResult => GetCpuInfoFrom(cpuInfoQueryResult))
            .ToList();
    }

    async static Task<PSDataCollection<PSObject>> WinQueryCpuInfoForAll()
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

    static CpuInfo GetCpuInfoFrom(PSObject powerShellQueryResult)
    {
        var id = powerShellQueryResult.Properties["ProcessorId"].Value.ToString()!;
        var name = powerShellQueryResult.Properties["Name"].Value.ToString()!;

        var currentClockSpeed = (uint)powerShellQueryResult.Properties["CurrentClockSpeed"].Value;
        var maxClockSpeed = (uint)powerShellQueryResult.Properties["MaxClockSpeed"].Value;
        var clockInfo = new CpuClockInfo(currentClockSpeed, maxClockSpeed);

        var coreCount = (uint)powerShellQueryResult.Properties["NumberOfCores"].Value;
        var threadCount = (uint)powerShellQueryResult.Properties["ThreadCount"].Value;
        var loadPercentage = (ushort)powerShellQueryResult.Properties["LoadPercentage"].Value;

        return new(id, name, coreCount, threadCount, clockInfo, loadPercentage);
    }

    async static Task<List<CpuInfo>> LinuxGetForAll()
    {
        return (await LinuxQueryCpuInfoForAll())
            .Select(cpuInfoQueryResult => GetCpuInfoFrom(cpuInfoQueryResult))
            .ToList();

    }

    async static Task<List<string>> LinuxQueryCpuInfoForAll()
    {
        var startInfo = new ProcessStartInfo("sudo dmidecode")
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            Arguments = "-t processor | egrep \"Handle|ID|Version|Max Speed|Current Speed|Core Count|Thread Count\""
        };
        return (await Process.Start(startInfo)!.StandardOutput.ReadToEndAsync())
            .Split("Handle").ToList();  // Likely makes a split per core rather than whole CPU.
    }

    static CpuInfo GetCpuInfoFrom(string linuxQueryResult)
    {
        var linuxQueryResults = linuxQueryResult.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        string id = linuxQueryResults[1].Value();
        var name = linuxQueryResults[2].Value();

        uint currentClockSpeed = 0, maxClockSpeed = 0;
        if (float.TryParse(linuxQueryResults[4].Value(true), out var rawCurrentClockSpeed))
        {
            currentClockSpeed = (uint)Math.Round(rawCurrentClockSpeed);
        }
        if (float.TryParse(linuxQueryResults[3].Value(true), out var rawMaxClockSpeed))
        {
            maxClockSpeed = (uint)Math.Round(rawMaxClockSpeed);
        }
        var clockInfo = new CpuClockInfo(currentClockSpeed, maxClockSpeed);

        uint.TryParse(linuxQueryResults[8].Value(), out var coreCount);
        uint.TryParse(linuxQueryResults[6].Value(), out var threadCount);
        ushort loadPercentage = default;

        return new(id, name, coreCount, threadCount, clockInfo, loadPercentage);
    }
}
