using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Models;

namespace Telegram.Controllers;

[ApiController]
[Route($"telegram/{{token}}/{PathFragment}")]
public class MyChatMemberController : ControllerBase
{
    internal const string PathFragment = "my_chat_member";

    [HttpPost("removed")]
    public void Removed([FromServices] TelegramBot bot)
    {
        var subscriber = HttpContext.GetUpdate().MyChatMember!.Chat.Id;

        if (bot.Subscriptions.Remove(subscriber))
            bot.Logger.LogInformation("Subscriber was removed: {Subscriber}", subscriber);
    }
}
