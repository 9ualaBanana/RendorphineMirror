﻿using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using Telegram.Infrastructure.Bot;

namespace Telegram.TrialUsers;

public class TrialUsersMediatorClient
{
    readonly HttpClient _httpClient;
    readonly ILogger _logger;

    public TrialUsersMediatorClient(
        HttpClient httpClient,
        IOptions<TrialUsersMediatorOptions> options,
        ILogger<TrialUsersMediatorClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new(options.Value.Host.GetLeftPart(UriPartial.Authority));
        _logger = logger;
    }

    internal async Task<bool> IsAuthenticatedAsync(ChatId chatId, string userId)
        => await IsAuthenticatedAsync(chatId, userId);
    internal async Task<bool> IsAuthenticatedAsync(string chatId, string userId)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,

            new UriBuilder
            {
                Path = new PathString("/authentication/check").ToUriComponent(),
                Query = QueryString.Create(new Dictionary<string, string?>()
                { ["identifier"] = chatId, ["platform"] = 0.ToString(), ["userid"] = userId }).ToUriComponent()
            }.Uri.PathAndQuery);

        return (await _httpClient.SendAsync(request)).IsSuccessStatusCode;
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
                    Path = new PathString("/authentication/telegram_user").ToUriComponent(),
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
    /// TODO: Return some enum value instead that will represent the server response.
    internal async Task<HttpResponseMessage> TryReduceQuotaAsync(string taskAction, string chatId, string userId)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,

            new UriBuilder()
            {
                Path = new PathString("/try_reduce_quota").ToUriComponent(),
                Query = QueryString.Create(new Dictionary<string, string?>()
                { ["taskaction"] = taskAction, ["identifier"] = chatId, ["platform"] = 0.ToString(), ["userid"] = userId }).ToUriComponent()
            }.Uri.PathAndQuery);

        return await _httpClient.SendAsync(request);
    }
}
