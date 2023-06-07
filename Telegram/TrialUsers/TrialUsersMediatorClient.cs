using Microsoft.Extensions.Options;
using Telegram.Infrastructure.Bot;

namespace Telegram.TrialUsers;

public class TrialUsersMediatorClient
{
    readonly HttpClient _httpClient;
    readonly TrialUsersMediatorOptions _options;

    public TrialUsersMediatorClient(HttpClient httpClient, IOptions<TrialUsersMediatorOptions> options)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new(options.Value.Host.GetLeftPart(UriPartial.Authority));
        _options = options.Value;
    }

    internal async Task AuthenticateAsync(
        string chatId,
        TelegramBot.User.LoginWidgetData telegramUserLoginWidgetData)
    {
        var authenticationRequest = new HttpRequestMessage(
            HttpMethod.Get,

            new UriBuilder
            {
                Path = new PathString("/authenticate/telegram_user").ToUriComponent(),
                Query = telegramUserLoginWidgetData.ToQueryString().Add("chatid", chatId).ToUriComponent(),
            }.Uri.PathAndQuery
            );
        await _httpClient.SendAsync(authenticationRequest);
    }
}
