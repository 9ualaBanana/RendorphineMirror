using Telegram.Bot.Types;
using Telegram.Telegram.Authentication.Models;
using Telegram.Telegram.Authentication.Services;

namespace Telegram.Telegram.Updates;

public abstract class AuthenticatedTelegramCallbackQueryHandlerBase : TelegramCallbackQueryHandlerBase
{
    protected readonly ChatAuthenticator Authenticator;


    public AuthenticatedTelegramCallbackQueryHandlerBase(ILogger logger, TelegramBot bot, ChatAuthenticator authenticator)
        : base(logger, bot)
    {
        Authenticator = authenticator;
    }


    public override async Task HandleAsync(Update update)
    {
        var chatId = update.Message?.Chat.Id ?? update.CallbackQuery!.Message!.Chat.Id;
        var authenticationToken = Authenticator.TryGetTokenFor(chatId);

        if (authenticationToken is not null) await HandleAsync(update, authenticationToken);
    }

    protected abstract Task HandleAsync(Update update, ChatAuthenticationToken authenticationToken);
}
