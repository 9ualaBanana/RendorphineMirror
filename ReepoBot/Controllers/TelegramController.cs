using Microsoft.AspNetCore.Mvc;
using ReepoBot.Services.Telegram;
using Telegram.Bot.Types;

namespace ReepoBot.Controllers;

[Route("telegram")]
[ApiController]
public class TelegramController : ControllerBase
{
    // Telegram.Bot works only with Newtonsoft.
    [HttpPost]
    public void ReceiveUpdate(
        [FromBody] Update update,
        [FromServices] TelegramUpdateHandler telegramUpdateHandler,
        [FromServices] ILogger<TelegramController> logger)
    {
        logger.LogDebug("Update with {Type} is received", update.Type);
        try
        {
            telegramUpdateHandler.HandleAsync(update);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{updateType} couldn't be handled", update.Type);
        }
        logger.LogDebug("Update is successfully handled");
    }
}
