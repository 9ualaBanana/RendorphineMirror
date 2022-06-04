using ReepoBot.Services.Node;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramHelper;

namespace ReepoBot.Services.Telegram.UpdateHandlers;

internal class CommandTelegramUpdateHandler : ITelegramUpdateHandler
{
    readonly ILogger _logger;
    readonly TelegramBot _bot;
    readonly NodeSupervisor _nodeSupervisor;

    internal CommandTelegramUpdateHandler(
        ILoggerFactory loggerFactory,
        TelegramBot bot,
        NodeSupervisor nodeSupervisor)
    {
        _logger = loggerFactory.CreateLogger<CommandTelegramUpdateHandler>();
        _bot = bot;
        _nodeSupervisor = nodeSupervisor;
    }

    public async Task HandleAsync(Update update)
    {
        _logger.LogDebug("Dispatching bot command...");
        var command = update.Message!.Text![1..];
        switch (command)
        {
            case "ping":
                await HandlePing();
                break;
        };
    }

    async Task HandlePing()
    {
        _logger.LogDebug("Building the message with online nodes status...");
        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("*Node* | *Uptime*");
        messageBuilder.AppendLine("------------------------------------------------");
        foreach (var(node, _) in _nodeSupervisor.NodesOnline)
        {
            var uptime = _nodeSupervisor.GetUptimeFor(node);
            // It could already go offline.
            if (uptime is null) continue;
            var escapedUptime = $"{uptime:d\\.hh\\:mm}";
            messageBuilder.AppendLine($"*{node.Name}* (v.{node.Version}) *{node.IP}* | {escapedUptime}");
        }
        var message = messageBuilder.ToString().Sanitize();
        _logger.LogDebug("Sending the message to subscribers...");
        try
        {
            foreach (var subscriber in _bot.Subscriptions)
            {
                await _bot.SendTextMessageAsync(
                    subscriber,
                    message,
                    parseMode: ParseMode.MarkdownV2);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong when trying to send the message.");
        }
    }
}
