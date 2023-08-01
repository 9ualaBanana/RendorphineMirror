using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.Localization.Resources;
using Telegram.MPlus.Clients;
using Telegram.MPlus.Security;

namespace Telegram.Commands.Handlers;

public class MeCommand : CommandHandler
{
    readonly MPlusClient _mPlusClient;
    readonly LocalizedText.Authentication _localizedAuthenticationText;

    public MeCommand(
        MPlusClient mPlusClient,
        LocalizedText.Authentication localizedAuthenticationMessage,
        Command.Factory commandFactory,
        Command.Received receivedCommand,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger logger)
        : base(commandFactory, receivedCommand, bot, httpContextAccessor, logger)
    {
        _mPlusClient = mPlusClient;
        _localizedAuthenticationText = localizedAuthenticationMessage;
    }

    internal override Command Target => CommandFactory.Create("me");

    protected override async Task HandleAsync(Command receivedCommand)
    {
        var email = (await _mPlusClient.TaskManager
            .GetPublicSessionInfoAsync(MPlusIdentity.SessionIdOf(User), RequestAborted))
            .Email;
        var balance = await _mPlusClient.TaskLauncher.RequestBalanceAsync(MPlusIdentity.SessionIdOf(User), RequestAborted);
        await Bot.SendMessageAsync_(ChatId, $"Email: {email}\nBalance: {balance}");
    }
}
