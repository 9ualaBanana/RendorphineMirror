﻿using System.ComponentModel;
using System.Management;
using System.Runtime.Versioning;
using System.Text;
using UnitsNet;

namespace Hardware.MessageBuilders;

[SupportedOSPlatform("windows")]
internal class WindowsHardwareInfoMessageBuilder
{
    readonly HardwareInfo _hardwareInfo;

    internal WindowsHardwareInfoMessageBuilder(HardwareInfo hardwareInfo)
    {
        _hardwareInfo = hardwareInfo;
    }

    internal string Build(bool verbose = false)
    {
        if (!verbose)
        {
            var pcName = (_hardwareInfo.CPU.Components[0] as ManagementObject)?["SystemName"];
            var ip = ((_hardwareInfo.Network.Components[1] as ManagementBaseObject)?["IPAddress"] as string[])?[0];
            return $"{pcName}: {ip}".Sanitize();
        }

        var message = new StringBuilder();

        message.AppendLine(BuildCPUInfoMessage(_hardwareInfo.CPU));
        message.AppendLine(BuildGPUInfoMessage(_hardwareInfo.GPU));
        message.AppendLine(BuildRAMInfoMessage(_hardwareInfo.RAM));
        message.AppendLine(BuildDisksInfoMessage(_hardwareInfo.Disks));

        return message.ToString();
    }

    static string BuildCPUInfoMessage(Container cpuUnitsInfo)
    {
        var result = new StringBuilder();
        result.AppendLine("*CPU:*");
        result.AppendLine("---------".Sanitize());
        foreach (var cpuInfoComponent in cpuUnitsInfo.Components)
        {
            var cpuInfo = (cpuInfoComponent as ManagementBaseObject)!;
            result.AppendLine($"{cpuInfo["Name"]}  [ *{cpuInfo["NumberOfCores"]}* cores | *{cpuInfo["ThreadCount"]}* threads ]".Sanitize());
            result.AppendLine();
            result.AppendLine($"*Clock*: *{cpuInfo["CurrentClockSpeed"]}* MHz / *{cpuInfo["MaxClockSpeed"]}* MHz");
            result.AppendLine($"*Load*: *{cpuInfo["LoadPercentage"]}* %");
            result.AppendLine();
        }
        return result.ToString();
    }

    static string BuildGPUInfoMessage(IEnumerable<Dictionary<string, object>> gpuUnitsInfo)
    {
        var result = new StringBuilder();
        result.AppendLine("*GPU:*");
        result.AppendLine("---------".Sanitize());
        foreach (var gpuInfo in gpuUnitsInfo)
        {
            result.AppendLine($"{gpuInfo["Name"]}  [ *{gpuInfo["UsedMemory"]}* MB / *{gpuInfo["TotalMemory"]}* MB ]".Sanitize());
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
        result.AppendLine("---------".Sanitize());
        foreach (var ramInfoComponent in ramInfoForAll.Components)
        {
            var ramInfo = (ramInfoComponent as ManagementBaseObject)!;
            if (double.TryParse(ramInfo["Capacity"].ToString(), out var capacity))
            {
                capacity = Information.FromBytes(capacity).Gigabytes;
            }
            result.AppendLine($"{ramInfo["DeviceLocator"]} [ *{capacity:#}* GB | *{ramInfo["Speed"]}* MHz ]".Sanitize());
            result.AppendLine();
        }
        return result.ToString();
    }

    static string BuildDisksInfoMessage(Container diskInfoForAll)
    {
        var result = new StringBuilder();
        result.AppendLine("*Disks:*");
        result.AppendLine("---------".Sanitize());
        foreach (var diskInfoComponent in diskInfoForAll.Components)
        {
            var diskInfo = (diskInfoComponent as ManagementBaseObject)!;
            ulong usedStorageSpace = default;
            if (ulong.TryParse(diskInfo["Size"].ToString(), out var size) && ulong.TryParse(diskInfo["FreeSpace"].ToString(), out var freeSpace))
            {
                usedStorageSpace = size - freeSpace;
            }
            result.AppendLine($"{diskInfo["Caption"]}  [ *{Information.FromBytes((double)usedStorageSpace).Gigabytes:.#}* GB /" +
                              $" *{Information.FromBytes((double)size).Gigabytes:.#}* GB ]".Sanitize());
            result.AppendLine();
        }
        return result.ToString();
    }
}