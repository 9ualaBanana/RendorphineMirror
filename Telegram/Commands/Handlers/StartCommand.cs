using Telegram.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.MPlus;
using Telegram.Persistence;
using Telegram.Security.Authentication;

namespace Telegram.Commands.Handlers;

public class StartCommand : CommandHandler
{
    readonly LoginManager _loginManager;
    readonly MPlusClient _mPlusClient;
    readonly TelegramBotDbContext _database;

    public StartCommand(
        LoginManager loginManager,
        MPlusClient mPlusClient,
        TelegramBotDbContext database,
        Command.Factory commandFactory,
        Command.Received receivedCommand,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<StartCommand> logger)
        : base(commandFactory, receivedCommand, bot, httpContextAccessor, logger)
    {
        _loginManager = loginManager;
        _mPlusClient = mPlusClient;
        _database = database;
    }

    internal override Command Target => CommandFactory.Create("start");

    protected override async Task HandleAsync(Command receivedCommand)
    {
        if (receivedCommand.UnquotedArguments.FirstOrDefault() is string sessionId)
        {
            var publicSessionInfo = await _mPlusClient.TaskManager.GetPublicSessionInfoAsync(sessionId, RequestAborted);

            var user = await _database.FindOrAddUserAsyncWith(ChatId, RequestAborted);

            if (user.MPlusIdentity is null)
            {
                await _loginManager.PersistMPlusUserIdentityAsync(user, new(publicSessionInfo.ToMPlusIdentity()), save: true, RequestAborted);
                await _loginManager.SendSuccessfulLogInMessageAsync(ChatId, sessionId, RequestAborted);
            }
            else await Bot.SendMessageAsync_(ChatId,
                "You are already logged in.",
                cancellationToken: RequestAborted);
        }
        else
        {
            var exception = new ArgumentNullException(nameof(sessionId),
                $"Required {nameof(sessionId)} argument to {Target.Prefixed} is missing.");
            Logger.LogCritical(exception, "Pass-through M+ authentication failed.");
            throw exception;
        }
    }
}
