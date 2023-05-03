using Microsoft.AspNetCore.Authorization;
using Telegram.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.Infrastructure.Commands.SyntacticAnalysis;
using Telegram.Persistence;
using Telegram.Security.Authentication;
using Telegram.Security.Authorization;

namespace Telegram.Commands.Handlers;

public class LogoutCommand : CommandHandler, IAuthorizationPolicyProtected
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

    public AuthorizationPolicy AuthorizationPolicy { get; } = new MPlusAuthorizationPolicyBuilder().Build();

    protected override async Task HandleAsync(ParsedCommand receivedCommand)
    {
        if (await _database.FindAsync<TelegramBotUserEntity>(ChatId) is TelegramBotUserEntity user && user.MPlusIdentity is not null)
        {
            _database.Remove(user.MPlusIdentity);
            await _database.SaveChangesAsync(RequestAborted);

            await Bot.SendMessageAsync_(ChatId,
                "You are logged out now.",
                cancellationToken: RequestAborted);
        }
        // Unauthenticated users shall not be able to call this method due to the authorization requirements.
    }
}
