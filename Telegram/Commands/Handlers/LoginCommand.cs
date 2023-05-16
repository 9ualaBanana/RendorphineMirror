using System.Collections.Immutable;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Infrastructure.Commands;
using Telegram.Persistence;
using Telegram.Security.Authentication;

namespace Telegram.Commands.Handlers;

/// <remarks>
/// Each invocation of <see cref="LoginCommand"/> results in persisting <see cref="ChatId"/>
/// of the user who invoked it, if hasn't been persisted yet. Resulting <see cref="TelegramBotUserEntity"/>
/// will contain <see cref="TelegramBotUserEntity.MPlusIdentity"/> if the user is already logged in.
/// Otherwise, a login attempt is made using credentials provided by the user and <see cref="TelegramBotUserEntity"/>
/// will contain <see cref="TelegramBotUserEntity"/> if the attempt was successful.
/// </remarks>
public class LoginCommand : CommandHandler
{
    readonly LoginManager _loginManager;

    public LoginCommand(
        LoginManager loginManager,
        Command.Factory commandFactory,
        Command.Received receivedCommand,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<LoginCommand> logger)
        : base(commandFactory, receivedCommand, bot, httpContextAccessor, logger)
    {
        _loginManager = loginManager;
    }

    internal override Command Target => CommandFactory.Create("login");

    protected override async Task HandleAsync(Command receivedCommand)
    {
        var user = await _loginManager.PersistTelegramUserAsync(ChatId, save: true, RequestAborted);

        if (user.MPlusIdentity is not null) 
            await Bot.SendMessageAsync_(ChatId,
                $"You are already logged in.",
                cancellationToken: RequestAborted
                );
        else await TryLogInAsync(user);


        async Task TryLogInAsync(TelegramBotUserEntity user)
        {
            var arguments = receivedCommand.UnquotedArguments.ToImmutableArray();
            if (arguments.Length == 2)
            {
                (string email, string password) = (arguments.First(), arguments.Last());
                await _loginManager.TryLogInAsync(user, email, password, RequestAborted);
            }
            else await Bot.SendMessageAsync_(ChatId,
                $"Login must be performed like the following:\n" +
                $"`{Target.Prefixed} <email> <password>`",
                cancellationToken: RequestAborted);
        }
    }
}
