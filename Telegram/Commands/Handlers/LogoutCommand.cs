using Microsoft.AspNetCore.Authorization;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.Infrastructure.Persistence;
using Telegram.Localization.Resources;
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

    internal override Command Target => CommandFactory.Create("logout");

    public AuthorizationPolicy AuthorizationPolicy { get; } = new MPlusAuthorizationPolicyBuilder().Build();

    protected override async Task HandleAsync(Command receivedCommand)
    {
        if (await _authenticationManager.GetBotUserAsyncWith(ChatId) is TelegramBot.User.Entity user && user.IsAuthenticatedByMPlus)
        {
            _database.Remove(user.MPlusIdentity);
            await _database.SaveChangesAsync(RequestAborted);

            await Bot.SendMessageAsync_(ChatId,
                _localizedAuthenticationText.LoggedOut,
                cancellationToken: RequestAborted);
        }
    }
}
