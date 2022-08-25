using Telegram.Bot.Types;
using Telegram.Services.Telegram.Authentication;

namespace Telegram.Services.Telegram.Updates.Commands;

public class LoginCommand : Command
{
    readonly TelegramChatIdAuthenticator _authenticator;
    public LoginCommand(ILogger<LoginCommand> logger, TelegramBot bot, TelegramChatIdAuthenticator authenticator)
        : base(logger, bot)
    { _authenticator = authenticator; }

    public override string Value => "login";

    public override async Task HandleAsync(Update update)
    {
        await _authenticator.AuthenticateAsync(update.Message!);
    }
}
