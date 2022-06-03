using Microsoft.AspNetCore.Mvc;
using ReepoBot.Models;
using ReepoBot.Services.Node;

namespace ReepoBot.Controllers;

[Route("node")]
[ApiController]
public class NodeController : ControllerBase
{
    [HttpGet("ping")]
    public async Task UpdateNodeStatus(
        [FromQuery] NodeInfo nodeInfo,
        [FromServices] NodeSupervisor nodeSupervisor,
        [FromServices] ILogger<NodeController> logger)
    {
        logger.LogDebug("Received ping from {name} (v.{version}).", nodeInfo.Name, nodeInfo.Version);
        await nodeSupervisor.HandleAsync(nodeInfo);
    }


    [HttpPost("hardware_info")]
    public async Task ForwardHardwareInfoMessageToTelegramAsync(
        [FromBody] string hardwareInfoMessage,
        [FromServices] HardwareInfoForwarder hardwareInfoForwarder,
        [FromServices] ILogger<NodeController> logger
        )
    {
        logger.LogDebug("Hardware info message is received.");
        try
        {
            await hardwareInfoForwarder.HandleAsync(hardwareInfoMessage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{receiver} couldn't forward following hardware info message:\n{message}",
                nameof(ForwardHardwareInfoMessageToTelegramAsync), hardwareInfoMessage);
        }
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
            logger.LogError(ex, "\"{configKey}\" config key is not defined.", configKey);
        }
        catch (FormatException ex)
        {
            logger.LogError(ex, "Value of \"{configKey}\" can't be parsed as bool.", configKey);
        }
        return false;
    }
}
