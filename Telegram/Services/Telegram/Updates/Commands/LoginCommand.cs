using Telegram.Bot.Types;
using Telegram.Services.Telegram.Authentication;

namespace Telegram.Services.Telegram.Updates.Commands;

public class LoginCommand : Command
{
    public LoginCommand(ILogger<LoginCommand> logger, TelegramBot bot, TelegramChatIdAuthentication authentication)
        : base(logger, bot, authentication)
    {
    }

    public override string Value => "login";

    internal override async Task HandleAsync(Update update)
    {
        await Authentication.AuthenticateAsync(update.Message!);
    }
}
