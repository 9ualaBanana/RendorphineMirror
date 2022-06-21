using Machine;
using Microsoft.AspNetCore.Mvc;
using Node.Profiler;
using ReepoBot.Models;
using ReepoBot.Services.Node;
using ReepoBot.Services.Telegram;
using System.Text;
using System.Text.Json;
using UnitsNet;

namespace ReepoBot.Controllers;

[Route("node")]
[ApiController]
public class NodeController : ControllerBase
{
    [HttpGet("ping")]
    public void UpdateNodeStatus(
        [FromQuery] MachineInfo.DTO nodeInfo,
        [FromServices] NodeSupervisor nodeSupervisor,
        [FromServices] ILogger<NodeController> logger)
    {
        logger.LogDebug("Received ping from {Node}", nodeInfo.GetBriefInfoMDv2());

        nodeSupervisor.UpdateNodeStatus(nodeInfo);
    }

    [HttpPost("profile")]
    public void ForwardNodeProfileMessageToTelegram(
        [FromForm] NodeProfile nodeProfile,
        [FromServices] TelegramBot bot,
        [FromServices] ILogger<NodeController> logger
        )
    {
        logger.LogDebug("Hardware info message is received");

        if (string.IsNullOrWhiteSpace(nodeProfile.Info)) return;
        var info = JsonSerializer.Deserialize<Info>(nodeProfile.Info, NodeProfile.JsonOptions);
        if (info is null) return;

        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine($"{info.Nickname} Info:");
        messageBuilder.AppendLine($"IP: {info.IP}:{info.Port}");

        if (!string.IsNullOrWhiteSpace(info.Hardware))
        {
            var hardware = JsonSerializer.Deserialize<BenchmarkResults>(info.Hardware, NodeProfile.JsonOptions);
            if (hardware is not null)
            {
                messageBuilder.AppendLine()
                    .AppendLine("CPU:")
                    .AppendLine($"Rating: {Information.FromBytes(hardware.CPU.Rating).Megabytes:.#}")
                    .AppendLine($"FFmpeg Rating: {Information.FromBytes(hardware.CPU.FFmpegRating).Megabytes:.#}")
                    .AppendLine($"Load: {hardware.CPU.Load:.#}")
                    .AppendLine("GPU:")
                    .AppendLine($"Rating: {Information.FromBytes(hardware.GPU.Rating).Megabytes:.#}")
                    .AppendLine($"FFmpeg Rating: {Information.FromBytes(hardware.GPU.FFmpegRating).Megabytes:.#}")
                    .AppendLine($"Load: {hardware.GPU.Load:.#}")
                    .AppendLine("RAM:")
                    .AppendLine($"Capacity: {Information.FromBytes((double)hardware.RAM.Total).Gigabytes:.#}")
                    .AppendLine($"Free: {Information.FromBytes((double)hardware.RAM.Free).Gigabytes:.#}")
                    .AppendLine("Drives:");

                foreach (var drive in hardware.Disks)
                {
                    messageBuilder.AppendLine($"Free Space: {Information.FromBytes((double)drive.FreeSpace).Gigabytes:.#}");
                    messageBuilder.AppendLine($"Write Speed: {Information.FromBytes((double)drive.WriteSpeed).Gigabytes:.#}");
                }
            }
        }

        bot.TryNotifySubscribers(messageBuilder.ToString(), logger);
    }

    [HttpPost("plugins")]
    public void ForwardPluginsInstalledOnNodeToTelegram(
        NodePlugins plugins,
        [FromServices] TelegramBot bot,
        [FromServices] ILogger<NodeController> logger)
    {
        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine($"Plugins installed on {plugins.NodeInfo.GetBriefInfoMDv2()}");
        messageBuilder.AppendLine(TelegramHelperExtensions.HorizontalDelimeter);
        foreach (var groupedPlugins in plugins.Plugins.GroupBy(nodePlugin => nodePlugin.Type))
        {
            messageBuilder.AppendLine($"{Enum.GetName(groupedPlugins.Key)}");
            foreach (var plugin in groupedPlugins)
            {
                messageBuilder
                    .AppendLine($"\tVersion: {plugin.Version}")
                    .AppendLine($"\tPath: {plugin.Path.Replace(@"\", @"\\")}");
            }
            messageBuilder.AppendLine();
        }

        bot.TryNotifySubscribers(messageBuilder.ToString(), logger);
    }

    [HttpGet("hardware_info/is_verbose")]
    public bool IsVerbose(
        [FromServices] IConfiguration configuration,
        [FromServices] ILogger<NodeController> logger)
    {
        const string configKey = "IsVerbose";
        try
        {
            return bool.Parse(configuration[configKey]);
        }
        catch (ArgumentNullException ex)
        {
            logger.LogError(ex, "\"{ConfigKey}\" config key is not defined", configKey);
        }
        catch (FormatException ex)
        {
            logger.LogError(ex, "Value of \"{ConfigKey}\" can't be parsed as bool", configKey);
        }
        return false;
    }
}
