using Microsoft.AspNetCore.Mvc;
using Telegram.Models;
using Telegram.Services.Node;
using Telegram.Telegram;
using Telegram.Telegram.Authentication.Services;

namespace Telegram.Controllers;

[Route("node")]
[ApiController]
public class NodeController : ControllerBase
{
    [HttpPost("ping")]
    public async Task UpdateNodeStatus(
        [FromBody] MachineInfo nodeInfo,
        [FromServices] UserNodes userNodes,
        [FromServices] ILogger<NodeController> logger,
        [FromServices] ILoggerFactory loggerFactory,
        [FromServices] IConfiguration configuration,
        [FromServices] TelegramBot bot,
        [FromServices] AuthentcatedUsersRegistry users)
    {
        logger.LogDebug("Received ping from {Node}", nodeInfo.BriefInfoMDv2);

        await userNodes.GetOrAdd(
            nodeInfo.UserId,
            _ => new NodeSupervisor(loggerFactory.CreateLogger<NodeSupervisor>(), configuration, bot, users)
            ).UpdateNodeStatusAsync(nodeInfo);
    }

    //[HttpPost("profile")]
    //public void ForwardNodeProfileMessageToTelegram(
    //    [FromForm] NodeProfile nodeProfile,
    //    [FromServices] TelegramBot bot,
    //    [FromServices] ILogger<NodeController> logger
    //    )
    //{
    //    logger.LogDebug("Hardware info message is received");

    //    if (string.IsNullOrWhiteSpace(nodeProfile.info)) return;
    //    var info = JsonSerializer.Deserialize<Info>(nodeProfile.info);
    //    if (info is null) return;

    //    var messageBuilder = new StringBuilder();
    //    messageBuilder.AppendLine($"{info.nickname} Info:");
    //    messageBuilder.AppendLine($"IP: {info.ip}:{info.port}");

    //    if (info.hardware is not null)
    //    {
    //        var hardware = info.hardware;
    //        if (hardware is not null)
    //        {
    //            messageBuilder.AppendLine()
    //                .AppendLine("CPU:")
    //                .AppendLine($"Rating: *{Information.FromBytes(hardware.cpu.rating).Megabytes:.#}* MB/s")
    //                .AppendLine($"FFmpeg Rating: *{Information.FromBytes(hardware.cpu.pratings["ffmpeg"]).Megabytes:.#}* MB/s")
    //                .AppendLine($"Load: *{hardware.cpu.load:.#}* %")
    //                .AppendLine()
    //                .AppendLine("GPU:")
    //                .AppendLine($"Rating: *{Information.FromBytes(hardware.gpu.rating).Megabytes:.#}* MB/s")
    //                .AppendLine($"FFmpeg Rating: *{Information.FromBytes(hardware.gpu.pratings["ffmpeg"]).Megabytes:.#}* MB/s")
    //                .AppendLine($"Load: *{hardware.gpu.load:.#}* %")
    //                .AppendLine()
    //                .AppendLine("RAM:")
    //                .AppendLine($"Capacity: *{Math.Floor(Information.FromBytes((double)hardware.ram.total).Gigabytes)}* GB")
    //                .AppendLine($"Free: *{Information.FromBytes((double)hardware.ram.free).Gigabytes:.#}* GB")
    //                .AppendLine()
    //                .AppendLine("Drives:");

    //            foreach (var drive in hardware.disks)
    //            {
    //                messageBuilder.AppendLine($"Free Space: *{Information.FromBytes((double)drive.freespace).Gigabytes:.#}* GB");
    //                messageBuilder.AppendLine($"Write Speed: *{Information.FromBytes((double)drive.writespeed).Megabytes:.#}* MB/s");
    //            }
    //        }
    //    }

    //    bot.TryNotifySubscribers(messageBuilder.ToString(), logger);
    //}

    //[HttpGet("hardware_info/is_verbose")]
    //public bool IsVerbose(
    //    [FromServices] IConfiguration configuration,
    //    [FromServices] ILogger<NodeController> logger)
    //{
    //    const string configKey = "IsVerbose";
    //    try
    //    {
    //        return bool.Parse(configuration[configKey]);
    //    }
    //    catch (ArgumentNullException ex)
    //    {
    //        logger.LogError(ex, "\"{ConfigKey}\" config key is not defined", configKey);
    //    }
    //    catch (FormatException ex)
    //    {
    //        logger.LogError(ex, "Value of \"{ConfigKey}\" can't be parsed as bool", configKey);
    //    }
    //    return false;
    //}
}
