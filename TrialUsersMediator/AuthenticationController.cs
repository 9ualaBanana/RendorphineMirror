using Microsoft.AspNetCore.Mvc;
using Telegram.Infrastructure.Bot;

namespace TrialUsersMediator;

[ApiController]
[Route("authenticate")]
public class AuthenticationController : ControllerBase
{
    [HttpPost("telegram_user")]
    public async Task Authenticate(
        [FromQuery] long chatId,
        [FromQuery] TelegramBot.User.LoginWidgetData telegramUserLoginWidgetData)
    {
        var client = TrialUser.From(Platform.Telegram).With(chatId);
    }
}