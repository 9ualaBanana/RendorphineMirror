using System.Collections.Immutable;
using Telegram.Bot.Types;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.Localization.Resources;
using Telegram.Security.Authentication;

namespace Telegram.Commands.Handlers;

/// <remarks>
/// Each invocation of <see cref="LoginCommand"/> results in persisting <see cref="ChatId"/>
/// of the user who invoked it, if hasn't been persisted yet. Resulting <see cref="TelegramBot.User.Entity"/>
/// will contain <see cref="TelegramBot.User.Entity.MPlusIdentity"/> if the user is already authenticated by M+.
/// Otherwise, an authentication attempt is made using credentials provided by the user and <see cref="TelegramBot.User.Entity"/>
/// will contain <see cref="TelegramBot.User.Entity.MPlusIdentity"/> if the attempt was successful.
/// </remarks>
public class LoginCommand : CommandHandler
{
    readonly AuthenticationManager _authenticationManager;
    readonly LocalizedText.Authentication _localizedAuthenticationMessage;

    public LoginCommand(
        AuthenticationManager authenticationManager,
        LocalizedText.Authentication localizedAuthenticationMessage,
        Command.Factory commandFactory,
        Command.Received receivedCommand,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<LoginCommand> logger)
        : base(commandFactory, receivedCommand, bot, httpContextAccessor, logger)
    {
        _authenticationManager = authenticationManager;
        _localizedAuthenticationMessage = localizedAuthenticationMessage;
    }

    internal override Command Target => CommandFactory.Create("login");

    protected override async Task HandleAsync(Command receivedCommand)
    {
        var user = await _authenticationManager.PersistTelegramUserAsyncWith(ChatId, save: false, RequestAborted);

        if (user.IsAuthenticatedByMPlus)
            await _authenticationManager.SendAlreadyLoggedInMessageAsync(ChatId, RequestAborted);
        else await TryAuthenticateByMPlusAsync(user);


        async Task TryAuthenticateByMPlusAsync(TelegramBot.User.Entity user)
        {
            var arguments = receivedCommand.UnquotedArguments.ToImmutableArray();
            if (arguments.Length == 2)
            {
                (string email, string password) = (arguments.First(), arguments.Last());
                await _authenticationManager.TryAuthenticateByMPlusAsync(user, email, password, RequestAborted);
            }
            else await Bot.SendMessageAsync_(ChatId,
                _localizedAuthenticationMessage.WrongSyntax(Target.Prefixed, correctSyntax: $"{Target.Prefixed} email password"),
                cancellationToken: RequestAborted);
        }
    }
}
