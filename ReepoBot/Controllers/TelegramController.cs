using Microsoft.AspNetCore.Mvc;
using ReepoBot.Services.Telegram;
using Telegram.Bot.Types;

namespace ReepoBot.Controllers;

[Route("telegram")]
[ApiController]
public class TelegramController : ControllerBase
{
    [HttpPost]
    public void ReceiveUpdate(
        [FromBody] Update update,
        [FromServices] TelegramUpdateHandler telegramUpdateHandler,
        [FromServices] ILogger<TelegramController> logger)
    {
        logger.LogDebug("Update of type {Type} is received", update.Type);
        bool isHandled = true;

        try { telegramUpdateHandler.Handle(update); }
        catch (Exception ex)
        {
            isHandled = false;
            logger.LogError(ex, "Update couldn't be handled");
        }

        if (isHandled) logger.LogDebug("Update was successfully handled");
    }
}
