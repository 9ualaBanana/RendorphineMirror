using ReepoBot.Models;
using ReepoBot.Services.Node;
using System.Text;
using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram.Updates;

public class TelegramCommandHandler
{
    readonly ILogger _logger;

    readonly TelegramBot _bot;
    readonly NodeSupervisor _nodeSupervisor;

    public TelegramCommandHandler(
        ILogger<TelegramMessageHandler> logger,
        TelegramBot bot,
        NodeSupervisor nodeSupervisor)
    {
        _logger = logger;
        _bot = bot;
        _nodeSupervisor = nodeSupervisor;
    }

    public void Handle(Update update)
    {
        var command = update.Message!.Text!;
        _logger.LogDebug("Dispatching {Command} command...", command);

        var unprefixedCommand = command[1..];
        if (unprefixedCommand.StartsWith("pinglist"))
            { HandlePingList(update); return; }
        else if (unprefixedCommand.StartsWith("ping"))
            { HandlePing(update); return; }
        else if (unprefixedCommand.StartsWith("plugins"))
            { HandlePlugins(update); return; }
        else if (unprefixedCommand.StartsWith("online"))
            { HandleOnline(update); return; }
        else if (unprefixedCommand.StartsWith("offline"))
            { HandleOffline(update); return; }
        else if (unprefixedCommand.StartsWith("remove"))
            { HandleRemove(update); return; }

        _logger.LogWarning("No handler for {Command} command is found", command);
    }

    void HandlePingList(Update update)
    {
        _logger.LogDebug("Listing all nodes...");

        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("*All Nodes*");
        messageBuilder.AppendLine(TelegramHelperExtensions.HorizontalDelimeter);
        foreach (var nodeInfo in _nodeSupervisor.AllNodes.OrderBy(node => node.NodeName))
        {
            messageBuilder.Append(nodeInfo.BriefInfoMDv2);
            if (_nodeSupervisor.NodesOffline.Contains(nodeInfo)) messageBuilder.AppendLine(" *--OFFLINE--*");
            else messageBuilder.AppendLine();
        }
        var message = messageBuilder.ToString();

        _ = _bot.TrySendMessageAsync(update.Message!.Chat.Id, message, _logger);
    }

    void HandlePing(Update update)
    {
        _logger.LogDebug("Listing online nodes...");

        IEnumerable<KeyValuePair<MachineInfo, TimerPlus>> nodesOnlineToList;
        var splitCommand = update.Message!.Text!.Split();

        if (splitCommand.Length > 1)
        {
            var lcNodeNamesToList = splitCommand[1..]
                .Select(nodeName => nodeName.ToLower())
                .ToHashSet();

            nodesOnlineToList = _nodeSupervisor.NodesOnline.Where(
                nodeInfo => lcNodeNamesToList.Any(
                    lcNodeNameToList => nodeInfo.Key.NodeName.ToLower().Contains(lcNodeNameToList)
                    )
                );
        }
        else
        {
            nodesOnlineToList = _nodeSupervisor.NodesOnline;
        }

        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("*Node* | *Uptime*");
        messageBuilder.AppendLine(TelegramHelperExtensions.HorizontalDelimeter);
        foreach (var (nodeInfo, _) in nodesOnlineToList.OrderBy(nodeOnline => nodeOnline.Key.NodeName))
        {
            var uptime = _nodeSupervisor.GetUptimeFor(nodeInfo);
            // It could already go offline.
            if (uptime is null) continue;

            var escapedUptime = $"{uptime:d\\.hh\\:mm}";
            messageBuilder.AppendLine($"{nodeInfo.BriefInfoMDv2} | {escapedUptime}");
        }
        var message = messageBuilder.ToString();

        _ = _bot.TrySendMessageAsync(update.Message!.Chat.Id, message, _logger);
    }

    void HandlePlugins(Update update)
    {
        var nodesNamesWhosePluginsToShow = update.Message!.Text!.Split('"', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[1..];
        var nodesWhosePluginsToShow = _nodeSupervisor.AllNodes.Where(node => node.NameContainsAny(nodesNamesWhosePluginsToShow));

        var messageBuilder = new StringBuilder();
        foreach (var node in nodesWhosePluginsToShow)
        {
            messageBuilder.AppendLine(node.InstalledPluginsAsText);
            messageBuilder.AppendLine();
        }

        _ = _bot.TrySendMessageAsync(update.Message!.Chat.Id, messageBuilder.ToString(), _logger);
    }

    void HandleOnline(Update update)
    {
        var message = $"Online: *{_nodeSupervisor.NodesOnline.Count}*\nOffline: {_nodeSupervisor.NodesOffline.Count}";
        _ = _bot.TrySendMessageAsync(update.Message!.Chat.Id, message, _logger);
    }

    void HandleOffline(Update update)
    {
        _logger.LogDebug("Listing offline nodes...");

        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("*Offline Nodes*");
        messageBuilder.AppendLine(TelegramHelperExtensions.HorizontalDelimeter);
        foreach (var nodeInfo in _nodeSupervisor.NodesOffline.OrderBy(node => node.NodeName))
        {
            messageBuilder.AppendLine(nodeInfo.BriefInfoMDv2);
        }
        var message = messageBuilder.ToString();

        _ = _bot.TrySendMessageAsync(update.Message!.Chat.Id, message, _logger);
    }

    void HandleRemove(Update update)
    {
        var nodeNames = update.Message!.Text!.Split('"', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[1..];

        int nodesRemoved = _nodeSupervisor.TryRemoveNodesWithNames(nodeNames);

        var message = nodesRemoved == 0 ?
            $"Nodes with specified names are either online or not found." :
            $"{nodesRemoved} were removed.";

        _ = _bot.TrySendMessageAsync(update.Message!.Chat.Id, message, _logger);
    }
}