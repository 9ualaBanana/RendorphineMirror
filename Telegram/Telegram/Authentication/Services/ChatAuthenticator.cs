﻿using System.Data.Entity;
using Telegram.Bot.Types;
using Telegram.Telegram.Authentication.Models;

namespace Telegram.Telegram.Authentication.Services;

public class ChatAuthenticator
{
    readonly ILogger _logger;

    readonly TelegramBot _bot;
    readonly HttpClient _httpClient;
    readonly AuthenticatedUsersDbContext _authenticatedUsers;


    public ChatAuthenticator(
        ILogger<ChatAuthenticator> logger,
        TelegramBot bot,
        IHttpClientFactory httpClientFactory,
        AuthenticatedUsersDbContext authenticatedUsers)
    {
        _logger = logger;
        _bot = bot;
        _httpClient = httpClientFactory.CreateClient();
        _authenticatedUsers = authenticatedUsers;
    }


    internal ChatAuthenticationToken? TryGetTokenFor(ChatId id)
    {
        if (IsAuthenticated(id)) return _authenticatedUsers.Users.AsNoTracking().Single(user => user.ChatId == id);

        _ = _bot.TrySendMessageAsync(id, "Authentication required."); return null;
    }

    internal async Task AuthenticateAsync(Message message)
    {
        if (IsAuthenticated(message.Chat.Id))
        { await _bot.TrySendMessageAsync(message.Chat.Id, "You are already authenticated."); return; }

        await TryAuthenticateAsyncFrom(message);
    }

    async Task TryAuthenticateAsyncFrom(Message message)
    {
        if (TelegramCredentials.TryParse(message, out var credentials))
        {
            if (await TryAuthenticateAsync(credentials!))
                await _bot.TrySendMessageAsync(message.Chat.Id, "You are successfully authenticated.");
            else
                await _bot.TrySendMessageAsync(message.Chat.Id, "Wrong credentials.");
        }
        else
        { await _bot.TrySendMessageAsync(message.Chat.Id, "Credentials are in a wrong format."); return; }
    }

    async Task<bool> TryAuthenticateAsync(TelegramCredentials credentials)
    {
        try
        {
            var mPlusAuthenticationToken = await AuthenticateAsync(credentials);
            _authenticatedUsers.Users.Add(new(credentials.ChatId, mPlusAuthenticationToken)); _authenticatedUsers.SaveChanges();
            if (mPlusAuthenticationToken.IsAdmin)
                _bot.Subscriptions.Add(long.Parse(credentials.ChatId!));

            _logger.LogDebug("User is authenticated: {Login}", credentials.Login);
            return true;
        }
        catch (HttpRequestException) { }
        catch (Exception ex) { _logger.LogError(ex, "Couldn't authenticate user due to unexpected error"); }
        return false;
    }

    async Task<MPlusAuthenticationToken> AuthenticateAsync(TelegramCredentials credentials)
    {
        var httpContent = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            ["email"] = credentials.Login,
            ["password"] = credentials.Password,
            ["guid"] = Guid.NewGuid().ToString()
        });
        var response = await _httpClient.PostAsync("https://tasks.microstock.plus/rphtaskmgr/login", httpContent);
        return (await Api.GetJsonFromResponseIfSuccessfulAsync(response)).ToObject<MPlusAuthenticationToken>()!;
    }

    internal async Task LogOutAsync(ChatId id)
    {
        if (!IsAuthenticated(id))
        { await _bot.TrySendMessageAsync(id, "You are not authenticated."); }
        else
        {
            _authenticatedUsers.Remove(_authenticatedUsers.Users.Single(user => user.ChatId == id)); _authenticatedUsers.SaveChanges();
            if (_bot.Subscriptions.Contains((long)id.Identifier!))
                _bot.Subscriptions.Remove((long)id.Identifier!);
            await _bot.TrySendMessageAsync(id, "You are successfully logged out.");
        }
    }

    bool IsAuthenticated(ChatId id) => _authenticatedUsers.Users.Any(user => user.ChatId == id);
}
