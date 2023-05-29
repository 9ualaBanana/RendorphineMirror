using Microsoft.AspNetCore.Mvc;
using Telegram.Infrastructure;

namespace Telegram.Security.Authentication;

[ApiController]
[Route($"telegram/{{token}}/{PathFragment}")]
public class MessageController : ControllerBase
{
    internal const string PathFragment = "message";

    [HttpPost]
    public async Task Handle([FromServices] IEnumerable<MessageHandler> messageHandlers)
    {
        if (messageHandlers.Switch(HttpContext.GetUpdate().Message!) is MessageHandler messageHandler)
            await messageHandler.HandleAsync();
    }
}
