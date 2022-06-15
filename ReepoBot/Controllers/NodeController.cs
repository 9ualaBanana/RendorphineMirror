using Hardware;
using Microsoft.AspNetCore.Mvc;
using ReepoBot.Services.Node;
using ReepoBot.Services.Telegram;
using System.Text;
using System.Text.Json;

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

    [HttpPost("hardware_info")]
    public void ForwardHardwareInfoMessageToTelegram(
        [FromForm] MachineInfoPayload hardwareInfoMessage,
        [FromServices] TelegramBot bot,
        [FromServices] ILogger<NodeController> logger
        )
    {
        logger.LogDebug("Hardware info message is received");

        
        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine($"{hardwareInfoMessage.nickname} Info:");
        messageBuilder.AppendLine($"IP: {hardwareInfoMessage.ip}:{hardwareInfoMessage.port}");

        if (!string.IsNullOrWhiteSpace(hardwareInfoMessage.hardware))
        {
            var hardware = JsonSerializer.Deserialize<BenchmarkResults>(hardwareInfoMessage.hardware)!;
            messageBuilder.AppendLine(hardware.cpu.ToString());
            messageBuilder.AppendLine(hardware.gpu.ToString());
            messageBuilder.AppendLine(hardware.ram.ToString());
            messageBuilder.AppendLine(hardware.disks.ToString());
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
