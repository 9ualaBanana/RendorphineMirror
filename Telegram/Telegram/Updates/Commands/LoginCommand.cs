using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Telegram.Authentication.Services;

namespace Telegram.Telegram.Updates.Commands;

public class LoginCommand : Command
{
    readonly ChatAuthenticator _authenticator;
    public LoginCommand(ILogger<LoginCommand> logger, TelegramBot bot, ChatAuthenticator authenticator)
        : base(logger, bot)
    { _authenticator = authenticator; }

    public override string Value => "login";

    public override async Task HandleAsync(Update update)
    {
        await _authenticator.AuthenticateAsync(update.Message!);
    }
}
