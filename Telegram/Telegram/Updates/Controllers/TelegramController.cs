using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace Telegram.Telegram.Updates.Controllers;

[Route("telegram")]
[ApiController]
public class TelegramController : ControllerBase
{
    [HttpPost]
    public async Task ReceiveUpdate(
        [FromBody] Update update,
        [FromServices] TelegramUpdateTypeHandler telegramUpdateTypeHandler,
        [FromServices] ILogger<TelegramController> logger)
    {
        try { await telegramUpdateTypeHandler.HandleAsync(update); }
        catch (Exception ex) { logger.LogError(ex, "Update couldn't be handled"); return; }
    }
}
