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
    readonly TelegramBot _bot;
    readonly TelegramBotDbContext _database;

    public MPlusAuthenticationHandler(
        TelegramBot bot,
        TelegramBotDbContext database,
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
        _bot = bot;
        _database = database;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Authentication middleware is invoked regardless of the current middleware pipeline so Update might not have been read.
        if (Context.ContainsUpdate())
            if (await User() is TelegramBotUserEntity loggedInUser)
            {
                Context.User.AddIdentity(loggedInUser.MPlusIdentity!.ToClaimsIdentity(ClaimsIssuer));
                return AuthenticateResult.Success(new(Context.User, MPlusAuthenticationDefaults.AuthenticationScheme));
            }

        return AuthenticateResult.NoResult();


        async Task<TelegramBotUserEntity?> User()
            => await _database.FindAsync<TelegramBotUserEntity>(Context.GetUpdate().ChatId()) is TelegramBotUserEntity user
            && user.MPlusIdentity is not null ?
            user : null;
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        await _bot.SendMessageAsync_(Context.GetUpdate().ChatId(), "You must be logged in.");
    }
}

static class TelegramChatAuthenticationHandlerHelpers
{
    internal static ChatId ChatId(this Update update) =>
        update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id;
}
