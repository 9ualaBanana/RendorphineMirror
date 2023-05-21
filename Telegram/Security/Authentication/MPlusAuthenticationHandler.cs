using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using Telegram.Bot;
using Telegram.Commands.Handlers;
using Telegram.Infrastructure;
using Telegram.Persistence;

namespace Telegram.Security.Authentication;

public class MPlusAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    readonly StartCommand _startCommandHandler;
    readonly AuthenticationManager _authenticationManager;
    readonly TelegramBot _bot;

    public MPlusAuthenticationHandler(
        StartCommand startCommandHandler,
        AuthenticationManager authenticationManager,
        TelegramBot bot,
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
        _startCommandHandler = startCommandHandler;
        _authenticationManager = authenticationManager;
        _bot = bot;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Authentication middleware is invoked regardless of the current middleware pipeline so Update might not have been read.
        if (Context.ContainsUpdate())
            if (await PersistedTelegramUser() is var user && user.IsAuthenticatedByMPlus)
            {
                Context.User.AddIdentity(user.MPlusIdentity.ToClaimsIdentity(ClaimsIssuer));
                return AuthenticateResult.Success(new(Context.User, MPlusAuthenticationDefaults.AuthenticationScheme));
            }

        return AuthenticateResult.NoResult();


        async Task<TelegramBotUserEntity> PersistedTelegramUser()
            => await _authenticationManager.PersistTelegramUserAsyncWith(Context.GetUpdate().ChatId(),
            save: true, Context.RequestAborted);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var startCommand = _startCommandHandler.Target.Prefixed;
        await _bot.SendMessageAsync_(Context.GetUpdate().ChatId(),
            $"You must be logged in.\n" +
            $"Use {startCommand} command to get help with that.");
    }
}
