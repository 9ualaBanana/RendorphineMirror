using GIBS.Authorization;
using GIBS.Commands;
using Microsoft.AspNetCore.Authorization;
using Telegram.Localization.Resources;
using Telegram.Persistence;
using Telegram.Security.Authentication;
using Telegram.Security.Authorization;

namespace Telegram.Commands.Handlers;

public class LogoutCommand : CommandHandler, IAuthorizationPolicyProtected
{
    readonly AuthenticationManager _authenticationManager;
    readonly TelegramBotDbContext _database;
    readonly LocalizedText.Authentication _localizedAuthenticationText;

    public LogoutCommand(
        AuthenticationManager authenticationManager,
        TelegramBotDbContext database,
        LocalizedText.Authentication localizedAuthenticationText,
        Command.Factory commandFactory,
        Command.Received receivedCommand,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<LogoutCommand> logger)
        : base(commandFactory, receivedCommand, bot, httpContextAccessor, logger)
    {
        _authenticationManager = authenticationManager;
        _database = database;
        _localizedAuthenticationText = localizedAuthenticationText;
    }

    public override Command Target => CommandFactory.Create("logout");

    public AuthorizationPolicy AuthorizationPolicy { get; } = new MPlusAuthorizationPolicyBuilder().Build();

    protected override async Task HandleAsync(Command receivedCommand)
    {
        if (await _authenticationManager.GetBotUserAsyncWith(ChatId) is TelegramBotUserEntity user && user.IsAuthenticatedByMPlus)
        {
            _database.Remove(user.MPlusIdentity);
            await _database.SaveChangesAsync(RequestAborted);

            await Bot.SendMessageAsync_(ChatId,
                _localizedAuthenticationText.LoggedOut,
                cancellationToken: RequestAborted);
        }
    }
}
