using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using Telegram.Services.Telegram.Updates;

namespace Telegram.Controllers;

[Route("telegram")]
[ApiController]
public class TelegramController : ControllerBase
{
    [HttpPost]
    public async Task ReceiveUpdate(
        [FromBody] Update update,
        [FromServices] TelegramUpdateHandler telegramUpdateHandler,
        [FromServices] ILogger<TelegramController> logger)
    {
        try { await telegramUpdateHandler.HandleAsync(update); }
        catch (Exception ex) { logger.LogError(ex, "Update couldn't be handled"); return; }
    }
}
