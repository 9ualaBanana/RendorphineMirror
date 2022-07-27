﻿using Microsoft.AspNetCore.Mvc;
using ReepoBot.Services.Telegram.Updates;
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
        try { telegramUpdateHandler.Handle(update); }
        catch (Exception ex) { logger.LogError(ex, "Update couldn't be handled"); return; }
    }
}
