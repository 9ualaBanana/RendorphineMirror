using Telegram.Bot.Types;
using Telegram.Services.Telegram.Authentication;

namespace Telegram.Services.Telegram.Updates;

public abstract class AuthenticatedTelegramUpdateHandler : TelegramUpdateHandler
{
    protected readonly TelegramChatIdAuthenticator Authenticator;



    public AuthenticatedTelegramUpdateHandler(ILogger logger, TelegramBot bot, TelegramChatIdAuthenticator authenticator)
        : base(logger, bot)
    { Authenticator = authenticator; }



    public override async Task HandleAsync(Update update)
    {
        var chatId = update.Message?.Chat.Id ?? update.CallbackQuery!.Message!.Chat.Id;
        var authenticationToken = Authenticator.TryGetTokenFor(chatId);

        if (authenticationToken is not null) await HandleAsync(update, authenticationToken);
    }

    protected abstract Task HandleAsync(Update update, TelegramAuthenticationToken authenticationToken);
}
