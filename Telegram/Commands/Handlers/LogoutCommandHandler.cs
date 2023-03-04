using Microsoft.AspNetCore.Authorization;
using Telegram.Bot;
using Telegram.Commands.SyntacticAnalysis;
using Telegram.Models;
using Telegram.Persistence;
using Telegram.Security.Authentication;
using Telegram.Security.Authorization;

namespace Telegram.Commands.Handlers;

public class LogoutCommandHandler : CommandHandler, IAuthorizationRequirementsProvider
{
    readonly TelegramBotDbContext _database;

    public LogoutCommandHandler(
        TelegramBotDbContext database,
        CommandParser parser,
        TelegramBot bot,
        ILogger<LogoutCommandHandler> logger) : base(parser, bot, logger)
    {
        _database = database;
    }

    internal override Command Target => "logout";

    public IEnumerable<IAuthorizationRequirement> Requirements => new IAuthorizationRequirement[]
    {
        MPlusAuthenticationRequirement.Instance
    };

    protected override async Task HandleAsync(HttpContext context, ParsedCommand receivedCommand)
    {
        if (await _database.FindAsync<TelegramBotUserEntity>(context.GetUpdate().ChatId()) is TelegramBotUserEntity user && user.MPlusIdentity is not null)
        {
            _database.Remove(user.MPlusIdentity);
            await _database.SaveChangesAsync(context.RequestAborted);

            await Bot.SendMessageAsync_(context.GetUpdate().ChatId(),
                "You are logged out now.",
                cancellationToken: context.RequestAborted);
        }
        // Unauthenticated users shall not be able to call this method due to the authorization requirements.
    }
}
