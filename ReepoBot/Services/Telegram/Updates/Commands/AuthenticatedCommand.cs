using ReepoBot.Services.Telegram.Authentication;
using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram.Updates.Commands;

public abstract class AuthenticatedCommand : Command
{
    public AuthenticatedCommand(
        ILogger<AuthenticatedCommand> logger,
        TelegramBot bot,
        TelegramChatIdAuthentication authentication) : base(logger, bot, authentication)
    {
    }

    internal override async Task HandleAsync(Update update)
    {
        var id = update.Message!.Chat.Id;
        var authenticationToken = Authentication.GetTokenFor(id);

        if (authenticationToken is not null) await HandleAsync(update, authenticationToken);
        else await Bot.TrySendMessageAsync(id, "Authentication required.");
    }

    protected abstract Task HandleAsync(Update update, TelegramAuthenticationToken authenticationToken);
}
