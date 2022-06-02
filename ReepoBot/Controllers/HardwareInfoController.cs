using Microsoft.AspNetCore.Mvc;
using ReepoBot.Services.Hardware;

namespace ReepoBot.Controllers;

[Route("hardware_info")]
[ApiController]
public class HardwareInfoController : ControllerBase
{
    [HttpPost]
    public async Task ForwardHardwareInfoMessageToTelegramAsync(
        [FromBody] string hardwareInfoMessage,
        [FromServices] HardwareInfoForwarder hardwareInfoForwarder,
        [FromServices] ILogger<HardwareInfoController> logger
        )
    {
        logger.LogDebug("Hardware info message is received.");
        try
        {
            await hardwareInfoForwarder.HandleAsync(hardwareInfoMessage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{receiver} couldn't handle following hardware info message:\n{message}",
                nameof(ForwardHardwareInfoMessageToTelegramAsync), hardwareInfoMessage);
        }
    }

    [HttpGet("is_verbose")]
    public bool IsVerbose(
        [FromServices] IConfiguration configuration,
        [FromServices] ILogger<HardwareInfoController> logger)
    {
        try
        {
            return bool.Parse(configuration["IsVerbose"]);
        }
        catch (ArgumentNullException ex)
        {
            logger.LogError(ex, "`IsVerbose` property is null.");
        }
        catch (FormatException ex)
        {
            logger.LogError(ex, "`IsVerbose` value can't be parsed as bool.");
        }
        return false;
    }
}
