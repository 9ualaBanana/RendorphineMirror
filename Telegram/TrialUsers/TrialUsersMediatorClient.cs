using Microsoft.Extensions.Options;
using Telegram.Infrastructure.Bot;

namespace Telegram.TrialUsers;

public class TrialUsersMediatorClient
{
    readonly HttpClient _httpClient;
    readonly TrialUsersMediatorOptions _options;
    readonly ILogger _logger;

    public TrialUsersMediatorClient(
        HttpClient httpClient,
        IOptions<TrialUsersMediatorOptions> options,
        ILogger<TrialUsersMediatorClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new(options.Value.Host.GetLeftPart(UriPartial.Authority));
        _options = options.Value;
        _logger = logger;
    }

    /// <returns>M+ session ID of a user authenticated using provided arguments.</returns>
    internal async Task<string> AuthenticateAsync(
        string chatId,
        TelegramBot.User.LoginWidgetData telegramUserLoginWidgetData)
    {

        try { return await AuthenticateAsyncCore(); }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Authentication of a trial user from Telegram ({Identifier}) failed.", chatId);
            throw;
        }


        async Task<string> AuthenticateAsyncCore()
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

    /// <summary>
    /// If <paramref name="userId"/> is the trial user mediator user ID and <paramref name="chatId"/> belongs to an authenticated trial user,
    /// reduces its quota for <paramref name="taskAction"/>.
    /// </summary>
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
