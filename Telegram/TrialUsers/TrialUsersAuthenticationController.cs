using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Telegram.Infrastructure.Bot;

namespace Telegram.TrialUsers;

[ApiController]
[Route("authenticate_trial_user")]
public class TrialUsersAuthenticationController : ControllerBase
{
    internal const string using_telegram_login_widget_data = "using_telegram_login_widget_data";
    [HttpGet(using_telegram_login_widget_data, Name = using_telegram_login_widget_data)]
    public async Task<RedirectResult> Authenticate(
        [FromQuery] string chatId,
        [FromQuery] TelegramBot.User.LoginWidgetData telegramUserLoginWidgetData,
        [FromServices] TrialUsersMediatorClient trialUsersMediator,
        [FromServices] IOptions<TelegramBot.Options> telegramBotOptions)
    {
        string sessionId = await trialUsersMediator.AuthenticateAsync(chatId, telegramUserLoginWidgetData);
        
        return Redirect(TelegramBotAuthenticationDeepLinkFor(sessionId));


        string TelegramBotAuthenticationDeepLinkFor(string sessionId)
            => new UriBuilder()
            {
                Scheme = "https",
                Host = new HostString("t.me").ToUriComponent(),
                Path = new PathString($"/{telegramBotOptions.Value.Username}").ToUriComponent(),
                Query = QueryString.Create("start", sessionId).ToUriComponent()
            }.ToString();
    }
}
