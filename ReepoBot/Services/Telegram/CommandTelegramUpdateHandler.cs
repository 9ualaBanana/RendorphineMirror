using ReepoBot.Services.Node;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramHelper;

namespace ReepoBot.Services.Telegram;

internal class CommandTelegramUpdateHandler : TelegramUpdateHandler
{
    readonly NodeSupervisor _nodeSupervisor;

    public CommandTelegramUpdateHandler(
        ILogger<CommandTelegramUpdateHandler> logger,
        TelegramBot bot,
        NodeSupervisor nodeSupervisor) : base(logger, bot)
    {
        _nodeSupervisor = nodeSupervisor;
    }

    public override async Task HandleAsync(Update update)
    {
        var command = update.Message!.Text![1..];
        switch (command)
        {
            case "ping":
                await HandlePing();
                break;
            default:
                LogUnresolvedEvent(update);
                break;
        };
    }

    async Task HandlePing()
    {
        Logger.LogDebug("Building the message with online nodes status...");
        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("Node | Elapsed since last ping");
        messageBuilder.AppendLine("-----------------------------------------------------------------");
        foreach (var node in _nodeSupervisor.NodesOnline)
        {
            var lastPingReceived = _nodeSupervisor.TimeBeforeNodeGoesOffline - node.Value.Interval;
            messageBuilder.AppendLine($"{node.Key.Name} (v.{node.Key.Version}) | {lastPingReceived}");
        }
        var message = messageBuilder.ToString().Sanitize();
        Logger.LogDebug("Sending the message to subscribers...");
        try
        {
            foreach (var subscriber in Bot.Subscriptions)
            {
                await Bot.SendTextMessageAsync(
                    subscriber,
                    message,
                    parseMode: ParseMode.MarkdownV2);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Something went wrong when trying to send the message.");
        }
    }
}
