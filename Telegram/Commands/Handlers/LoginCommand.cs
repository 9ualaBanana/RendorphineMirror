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
/// will contain <see cref="TelegramBotUserEntity.MPlusIdentity"/> if the user is already authenticated by M+.
/// Otherwise, an authentication attempt is made using credentials provided by the user and <see cref="TelegramBotUserEntity"/>
/// will contain <see cref="TelegramBotUserEntity.MPlusIdentity"/> if the attempt was successful.
/// </remarks>
public class LoginCommand : CommandHandler
{
    readonly AuthenticationManager _authenticationManager;

    public LoginCommand(
        AuthenticationManager authenticationManager,
        Command.Factory commandFactory,
        Command.Received receivedCommand,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<LoginCommand> logger)
        : base(commandFactory, receivedCommand, bot, httpContextAccessor, logger)
    {
        _authenticationManager = authenticationManager;
    }

    internal override Command Target => CommandFactory.Create("login");

    protected override async Task HandleAsync(Command receivedCommand)
    {
        var user = await _authenticationManager.PersistTelegramUserAsyncWith(ChatId, save: false, RequestAborted);

        if (user.IsAuthenticatedByMPlus)
            await _authenticationManager.SendAlreadyLoggedInMessageAsync(ChatId, RequestAborted);
        else await TryAuthenticateByMPlusAsync(user);


        async Task TryAuthenticateByMPlusAsync(TelegramBotUserEntity user)
        {
            var arguments = receivedCommand.UnquotedArguments.ToImmutableArray();
            if (arguments.Length == 2)
            {
                (string email, string password) = (arguments.First(), arguments.Last());
                await _authenticationManager.TryAuthenticateByMPlusAsync(user, email, password, RequestAborted);
            }
            else await Bot.SendMessageAsync_(ChatId,
                $"Login must be performed in the following way:\n" +
                $"`{Target.Prefixed} <email> <password>`",
                cancellationToken: RequestAborted);
        }
    }
}
