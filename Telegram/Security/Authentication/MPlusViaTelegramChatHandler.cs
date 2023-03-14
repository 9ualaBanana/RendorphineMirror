using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Infrastructure;
using Telegram.Persistence;

namespace Telegram.Security.Authentication;

public class MPlusViaTelegramChatHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    readonly TelegramBot _bot;
    readonly TelegramBotDbContext _database;
    readonly IHttpContextAccessor _httpContextAccessor;

    public MPlusViaTelegramChatHandler(
        TelegramBot bot,
        TelegramBotDbContext database,
        IHttpContextAccessor httpContextAccessor,
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock)
    {
        _bot = bot;
        _database = database;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var context = _httpContextAccessor.HttpContext!;
        // AuthenticationMiddleware is invoked regardless of the current middleware pipeline so Update might have been not read.
        if (context.ContainsUpdate())
            if (await _database.FindAsync<TelegramBotUserEntity>(context.GetUpdate().ChatId()) is TelegramBotUserEntity user)
                if (user.MPlusIdentity is not null)
                {
                    context.User.AddIdentity(user.MPlusIdentity.ToClaimsIdentity());
                    return AuthenticateResult.Success(new(context.User, MPlusViaTelegramChatDefaults.AuthenticationScheme));
                }

        return AuthenticateResult.NoResult();
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        await _bot.SendMessageAsync_(_httpContextAccessor.HttpContext!.GetUpdate().ChatId(), "You must be logged in.");
    }
}

static class TelegramChatAuthenticationHandlerHelpers
{
    internal static ChatId ChatId(this Update update) =>
        update.Message?.Chat.Id ?? update.CallbackQuery?.Message!.Chat.Id;
}
