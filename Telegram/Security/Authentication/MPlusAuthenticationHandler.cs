using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Infrastructure;
using Telegram.Persistence;

namespace Telegram.Security.Authentication;

public class MPlusAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    readonly LoginManager _loginManager;
    readonly TelegramBot _bot;

    public MPlusAuthenticationHandler(
        LoginManager loginManager,
        TelegramBot bot,
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
        _loginManager = loginManager;
        _bot = bot;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Authentication middleware is invoked regardless of the current middleware pipeline so Update might not have been read.
        if (Context.ContainsUpdate())
            if (await TelegramAuthenticatedUser() is TelegramBotUserEntity loggedInUser)
            {
                Context.User.AddIdentity(loggedInUser.MPlusIdentity!.ToClaimsIdentity(ClaimsIssuer));
                return AuthenticateResult.Success(new(Context.User, MPlusAuthenticationDefaults.AuthenticationScheme));
            }

        return AuthenticateResult.NoResult();


        async Task<TelegramBotUserEntity> TelegramAuthenticatedUser()
            => await _loginManager.PersistTelegramUserAsync(Context.GetUpdate().ChatId(),
            save: true, Context.RequestAborted);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        await _bot.SendMessageAsync_(Context.GetUpdate().ChatId(), "You must be logged in.");
    }
}
