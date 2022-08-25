using Telegram.Bot.Types;
using Telegram.Services.Telegram.Authentication;

namespace Telegram.Services.Telegram.Updates.Commands;

public class LogoutCommand : AuthenticatedCommand
{
    public LogoutCommand(ILogger<AuthenticatedCommand> logger, TelegramBot bot, TelegramChatIdAuthentication authentication)
        : base(logger, bot, authentication)
    {
    }

    public override string Value => "logout";

    protected override async Task HandleAsync(Update update, TelegramAuthenticationToken authenticationToken)
    {
        await Authentication.LogOutAsync(update.Message!.Chat.Id);
    }
}
