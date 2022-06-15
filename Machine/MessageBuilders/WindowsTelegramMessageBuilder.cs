using Common;
using System.ComponentModel;
using System.Management;
using System.Runtime.Versioning;
using System.Text;
using UnitsNet;

namespace Machine.MessageBuilders;

[SupportedOSPlatform("windows")]
internal class WindowsHardwareInfoMessage
{
    internal static string Build()
    {
        var message = new StringBuilder();

        message.AppendLine($"v.{Init.Version}");
        message.AppendLine();

        message.AppendLine(BuildCPUInfoMessage(CPU.Info()));
        message.AppendLine(BuildGPUInfoMessage(GPU.Info()));
        //message.AppendLine(BuildRAMInfoMessage(RAM.Info()));
        //message.AppendLine(BuildDisksInfoMessage(Disks.Info()));

        return message.ToString();
    }

    static string BuildCPUInfoMessage(Container cpuUnitsInfo)
    {
        var result = new StringBuilder();
        result.AppendLine("*CPU:*");
        result.AppendLine("---------");
        foreach (var cpuInfoComponent in cpuUnitsInfo.Components)
        {
            var cpuInfo = (ManagementBaseObject)cpuInfoComponent;
            result.AppendLine($"{cpuInfo["Name"]}  [ *{cpuInfo["NumberOfCores"]}* cores | *{cpuInfo["ThreadCount"]}* threads ]");
            result.AppendLine($"*Clock*: *{cpuInfo["CurrentClockSpeed"]}* MHz / *{cpuInfo["MaxClockSpeed"]}* MHz");
            result.AppendLine($"*Load*: *{cpuInfo["LoadPercentage"]}* %");
            result.AppendLine();
        }
        return result.ToString();
    }

    static string BuildGPUInfoMessage(IEnumerable<IDictionary<string, object>> gpuUnitsInfo)
    {
        var result = new StringBuilder();
        result.AppendLine("*GPU:*");
        result.AppendLine("---------");
        foreach (var gpuInfo in gpuUnitsInfo)
        {
            result.AppendLine($"{gpuInfo["Name"]}  [ *{gpuInfo["UsedMemory"]}* MB / *{gpuInfo["TotalMemory"]}* MB ]");
            result.AppendLine($"*Core Clock:* *{gpuInfo["CurrentCoreClock"]}* MHz / *{gpuInfo["MaxCoreClock"]}* MHz");
            result.AppendLine($"*Memory Clock:* *{gpuInfo["CurrentMemoryClock"]}* MHz / *{gpuInfo["MaxMemoryClock"]}* MHz");
            result.AppendLine();
        }
        return result.ToString();
    }

    static string BuildRAMInfoMessage(Container ramInfoForAll)
    {
        var result = new StringBuilder();
        result.AppendLine("*RAM:*");
        result.AppendLine("---------");
        foreach (var ramInfoComponent in ramInfoForAll.Components)
        {
            var ramInfo = (ManagementBaseObject)ramInfoComponent;
            if (double.TryParse(ramInfo["Capacity"].ToString(), out var capacity))
            {
                capacity = Information.FromBytes(capacity).Gigabytes;
            }
            result.AppendLine($"{ramInfo["DeviceLocator"]} [ *{capacity:#}* GB | *{ramInfo["Speed"]}* MHz ]");
            result.AppendLine();
        }
        return result.ToString();
    }

    static string BuildDisksInfoMessage(Container diskInfoForAll)
    {
        var result = new StringBuilder();
        result.AppendLine("*Disks:*");
        result.AppendLine("---------");
        foreach (var diskInfoComponent in diskInfoForAll.Components)
        {
            var diskInfo = (ManagementBaseObject)diskInfoComponent;
            ulong usedStorageSpace = default;
            if (ulong.TryParse(diskInfo["Size"].ToString(), out var size) && ulong.TryParse(diskInfo["FreeSpace"].ToString(), out var freeSpace))
            {
                usedStorageSpace = size - freeSpace;
            }
            result.AppendLine($"{diskInfo["Caption"]}  [ *{Information.FromBytes((double)usedStorageSpace).Gigabytes:.#}* GB /" +
                              $" *{Information.FromBytes((double)size).Gigabytes:.#}* GB ]");
            result.AppendLine();
        }
        return result.ToString();
    }
}