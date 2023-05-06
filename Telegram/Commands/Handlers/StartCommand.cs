using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Infrastructure.Commands;
using Telegram.MPlus;
using Telegram.Persistence;
using Telegram.Security.Authentication;

namespace Telegram.Commands.Handlers;

public class StartCommand : CommandHandler
{
    readonly LoginCommand _loginCommandHandler;
    readonly LoginManager _loginManager;
    readonly MPlusClient _mPlusClient;
    readonly TelegramBotDbContext _database;

    public StartCommand(
        LoginCommand loginCommandHandler,
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
        _loginCommandHandler = loginCommandHandler;
        _loginManager = loginManager;
        _mPlusClient = mPlusClient;
        _database = database;
    }

    internal override Command Target => CommandFactory.Create("start");

    protected override async Task HandleAsync(Command receivedCommand)
    {
        if (!receivedCommand.UnquotedArguments.Any())
            await SendStartMessageAsync();
        else if (receivedCommand.UnquotedArguments.FirstOrDefault() is string sessionId)
            await AuthenticateViaBrowserAsync(sessionId);
        else
        {
            var exception = new ArgumentNullException(nameof(sessionId),
                $"Required {nameof(sessionId)} argument to {Target.Prefixed} is missing.");
            Logger.LogCritical(exception, "M+ authentication via browser failed.");
            throw exception;
        }


        async Task SendStartMessageAsync()
        {
            string loginCommand = _loginCommandHandler.Target.Prefixed;
            await Bot.SendMessageAsync_(ChatId,
                $"To authenticate with M+ use {loginCommand} followed by email and password separated by space or the button below to authenticate via browser.",
                InlineKeyboardButton.WithUrl("Authenticate via browser", "https://microstock.plus/oauth2/authorize?clientid=003&state=_"),
                disableWebPagePreview: true, cancellationToken: RequestAborted);
        }

        async Task AuthenticateViaBrowserAsync(string sessionId)
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
    }
}
