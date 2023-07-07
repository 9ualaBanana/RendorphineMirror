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

    /// <returns>M+ session ID of a user authenticated using provided arguments.</returns>
    internal async Task<string> AuthenticateAsync(
        string chatId,
        TelegramBot.User.LoginWidgetData telegramUserLoginWidgetData)
    {
        var authenticationRequest = new HttpRequestMessage(
            HttpMethod.Get,

            new UriBuilder
            {
                Path = new PathString("/authenticate/telegram_user").ToUriComponent(),
                Query = telegramUserLoginWidgetData.ToQueryString().Add("chatid", chatId).ToUriComponent(),
            }.Uri.PathAndQuery);

        string sessionId = await
            (await _httpClient.SendAsync(authenticationRequest))
            .Content.ReadAsStringAsync();
        return sessionId;
    }
}
