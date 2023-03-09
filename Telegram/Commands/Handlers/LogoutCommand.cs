using Microsoft.AspNetCore.Authorization;
using Telegram.Bot;
using Telegram.Commands.SyntacticAnalysis;
using Telegram.Persistence;
using Telegram.Security.Authentication;
using Telegram.Security.Authorization;

namespace Telegram.Commands.Handlers;

public class LogoutCommand : CommandHandler, IAuthorizationRequirementsProvider
{
    readonly TelegramBotDbContext _database;

    public LogoutCommand(
        TelegramBotDbContext database,
        CommandParser parser,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<LogoutCommand> logger)
        : base(parser, bot, httpContextAccessor, logger)
    {
        _database = database;
    }

    internal override Command Target => "logout";

    public IEnumerable<IAuthorizationRequirement> Requirements { get; }
        = IAuthorizationRequirementsProvider.Provide(MPlusAuthenticationRequirement.Instance);

    protected override async Task HandleAsync(ParsedCommand receivedCommand, HttpContext context)
    {
        if (await _database.FindAsync<TelegramBotUserEntity>(Update.ChatId()) is TelegramBotUserEntity user && user.MPlusIdentity is not null)
        {
            _database.Remove(user.MPlusIdentity);
            await _database.SaveChangesAsync(context.RequestAborted);

            await Bot.SendMessageAsync_(Update.ChatId(),
                "You are logged out now.",
                cancellationToken: context.RequestAborted);
        }
        // Unauthenticated users shall not be able to call this method due to the authorization requirements.
    }
}
