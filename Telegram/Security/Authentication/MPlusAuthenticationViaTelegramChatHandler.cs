using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Models;

namespace Telegram.Security.Authentication;

public class MPlusAuthenticationViaTelegramChatHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    readonly TelegramBot _bot;
    readonly MPlusAuthenticationAgent _mPlusAuthenticationAgent;
    readonly UpdateContextCache _updateContextCache;

    public MPlusAuthenticationViaTelegramChatHandler(
        TelegramBot bot,
        MPlusAuthenticationAgent mPlusAuthenticationAgent,
        UpdateContextCache updateContextCache,
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock)
    {
        _bot = bot;
        _mPlusAuthenticationAgent = mPlusAuthenticationAgent;
        _updateContextCache = updateContextCache;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var updateContext = _updateContextCache.Retrieve();
        // Include authentication when /login command is invoked.
        if (CredentialsFromChat.TryParse(updateContext.Update.Message, out var credentialsFromChat))
        {
            if (await _mPlusAuthenticationAgent.TryLogInAsyncUsing(credentialsFromChat) is MPlusIdentity mPlusIdentity)
            {
                updateContext.User.AddIdentity(mPlusIdentity.ToClaimsIdentity());
                return AuthenticateResult.Success(new(updateContext.User, MPlusAuthenticationViaTelegramChatDefaults.AuthenticationScheme));
            }
            else Logger.LogError("M+ authentication result returned from the server was in a wrong format");
        }
        else Logger.LogTrace("Credentials couldn't be parsed due to them being in a wrong format");

        return AuthenticateResult.NoResult();
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        await _bot.SendMessageAsync_(_updateContextCache.Retrieve().Update.ChatId(), "You must be logged in.");
    }
}

static class TelegramChatAuthenticationHandlerHelpers
{
    internal static ChatId ChatId(this Update update) =>
        update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id;
}
