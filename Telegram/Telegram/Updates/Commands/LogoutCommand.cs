using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Telegram.Authentication.Models;
using Telegram.Telegram.Authentication.Services;

namespace Telegram.Telegram.Updates.Commands;

public class LogoutCommand : AuthenticatedCommand
{
    public LogoutCommand(ILogger<AuthenticatedCommand> logger, TelegramBot bot, ChatAuthenticator authenticator)
        : base(logger, bot, authenticator)
    {
    }



    public override string Value => "logout";

    protected override async Task HandleAsync(Update update, ChatAuthenticationToken authenticationToken)
    {
        await Authenticator.LogOutAsync(update.Message!.Chat.Id);
    }
}
