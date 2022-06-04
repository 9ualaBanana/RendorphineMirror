using Microsoft.AspNetCore.Mvc;
using ReepoBot.Controllers.Binders;
using ReepoBot.Services.Telegram.UpdateHandlers;
using Telegram.Bot.Types;

namespace ReepoBot.Controllers;

[Route("telegram")]
[ApiController]
public class TelegramController : ControllerBase
{
    // Telegram.Bot works only with Newtonsoft.
    [HttpPost]
    public async Task ReceiveUpdate(
        [NewtonsoftJsonBinder] Update update,
        [FromServices] TelegramUpdateHandler telegramUpdateHandler,
        [FromServices] ILogger<TelegramController> logger)
    {
        try
        {
            await telegramUpdateHandler.HandleAsync(update);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{updateType} couldn't be handled", update.Type);
        }
    }
}
