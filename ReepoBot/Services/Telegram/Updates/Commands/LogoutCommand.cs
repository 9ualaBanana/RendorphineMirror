using ReepoBot.Services.Telegram.Authentication;
using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram.Updates.Commands;

public class LogoutCommand : AuthenticatedCommand
{
    public LogoutCommand(ILogger<AuthenticatedCommand> logger, TelegramBot bot, TelegramChatIdAuthentication authentication)
        : base(logger, bot, authentication)
    {
    }

    public override string Value => "logout";

    protected override async Task HandleAsync(Update update, TelegramAuthenticationToken authenticationToken)
    {
        Authentication.LogOut(update.Message!.Chat.Id);
    }
}
