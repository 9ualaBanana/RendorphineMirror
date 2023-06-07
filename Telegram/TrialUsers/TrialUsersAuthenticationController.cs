using Microsoft.AspNetCore.Mvc;
using Telegram.Infrastructure.Bot;

namespace Telegram.TrialUsers;

[ApiController]
[Route("authenticate_trial_user")]
public class TrialUsersAuthenticationController : ControllerBase
{
    internal const string using_telegram_login_widget_data = "using_telegram_login_widget_data";
    [HttpGet(using_telegram_login_widget_data, Name = using_telegram_login_widget_data)]
    public async Task Authenticate(
        [FromQuery] string chatId,
        [FromQuery] TelegramBot.User.LoginWidgetData telegramUserLoginWidgetData,
        [FromServices] TrialUsersMediatorClient trialUsersMediator)
    {
        await trialUsersMediator.AuthenticateAsync(chatId, telegramUserLoginWidgetData);
    }
}
