using Telegram.Bot.Types;
using Telegram.Services.Telegram.Authentication;

namespace Telegram.Services.Telegram.Updates.Commands;

public class LogoutCommand : AuthenticatedCommand
{
    public LogoutCommand(ILogger<AuthenticatedCommand> logger, TelegramBot bot, TelegramChatIdAuthenticator authenticator)
        : base(logger, bot, authenticator)
    {
    }



    public override string Value => "logout";

    protected override async Task HandleAsync(Update update, TelegramAuthenticationToken authenticationToken)
    {
        await Authenticator.LogOutAsync(update.Message!.Chat.Id);
    }
}
