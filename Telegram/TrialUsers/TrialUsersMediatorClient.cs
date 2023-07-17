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

    internal async Task TryReduceQuotaAsync(string taskAction, string chatId, string userId)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,

            new UriBuilder()
            {
                Path = new PathString("/try_reduce_quota").ToUriComponent(),
                Query = QueryString.Create(new Dictionary<string, string?>()
                { ["taskaction"] = taskAction, ["identifier"] = chatId, ["platform"] = 0.ToString(), ["userid"] = userId }).ToUriComponent()
            }.Uri.PathAndQuery);

        await _httpClient.SendAsync(request);
    }
}
