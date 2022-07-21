using ReepoBot.Models;
using ReepoBot.Services.Node;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ReepoBot.Services.Telegram;

public class TelegramUpdateHandler
{
    readonly ILogger _logger;
    readonly TelegramBot _bot;
    readonly NodeSupervisor _nodeSupervisor;

    public TelegramUpdateHandler(
        TelegramBot bot,
        NodeSupervisor nodeSupervisor,
        ILogger<TelegramUpdateHandler> logger)
    {
        _logger = logger;
        _nodeSupervisor = nodeSupervisor;
        _bot = bot;
    }

    internal void Handle(Update update)
    {
        _logger.LogDebug("Dispatching {Update}...", nameof(Update));

        switch (update.Type)
        {
            case UpdateType.Message:
                HandleMessageUpdate(update);
                break;
            case UpdateType.MyChatMember:
                HandleChatMember(update.MyChatMember!);
                break;
            default:
                _logger.LogWarning("Unsupported update type: {Type}", update.Type);
                break;
        }
    }

    void HandleMessageUpdate(Update update)
    {
        var message = update.Message!;
        _logger.LogDebug("Dispatching {Message}...", nameof(Message));
        if (IsCommand(message))
        {
            HandleCommandUpdate(update);
            return;
        }
        else if (IsSystemMessage(message))
        {
            _logger.LogDebug("System messages are handled by {Handler}", nameof(HandleChatMember));
            return;    // Bot adding and removal are handled via `UpdateType.MyChatMember` updates.
        }

        _logger.LogWarning("The following message couldn't be handled:\n{Message}", message.Text);
    }

    static bool IsCommand(Message message)
    {
        var messageText = message.Text;
        return messageText is not null && messageText.StartsWith('/') && messageText.Length > 1;
    }

    bool IsSystemMessage(Message message)
    {
        return message.LeftChatMember?.Id == _bot.BotId || message.NewChatMembers?.First().Id == _bot.BotId;
    }



    void HandleCommandUpdate(Update update)
    {
        var command = update.Message!.Text!;
        _logger.LogDebug("Dispatching {Command} command...", command);
        var unprefixedCommand = command[1..];

        if (unprefixedCommand.StartsWith("pinglist"))
        {
            HandlePingList(update);
            return;
        }
        else if (unprefixedCommand.StartsWith("ping"))
        {
            HandlePing(update);
            return;
        }
        else if (unprefixedCommand.StartsWith("plugins"))
        {
            HandlePlugins(update);
            return;
        }
        else if (unprefixedCommand.StartsWith("online"))
        {
            HandleOnline(update);
            return;
        }
        else if (unprefixedCommand.StartsWith("offline"))
        {
            HandleOffline(update);
            return;
        }
        else if (unprefixedCommand.StartsWith("remove"))
        {
            HandleRemove(update);
            return;
        }

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

    void HandleChatMember(ChatMemberUpdated chatMemberUpdate)
    {
        _logger.LogDebug("Dispatching {ChatMemberUpdated}...", nameof(ChatMemberUpdated));

        if (BotIsAddedToChat(chatMemberUpdate))
        {
            HandleBotIsAddedToChatAsync(chatMemberUpdate);
            return;
        }
        else if (BotIsRemovedFromChat(chatMemberUpdate))
        {
            HandleBotIsRemovedFromChatAsync(chatMemberUpdate);
            return;
        }
    }

    void HandleBotIsAddedToChatAsync(ChatMemberUpdated chatMemberUpdate)
    {
        var subscriber = chatMemberUpdate.Chat.Id;
        _logger.LogDebug("Adding new subscriber: {Subscriber}", subscriber);

        var subscribersCount = _bot.Subscriptions.Count;
        _bot.Subscriptions.Add(subscriber);

        if (_bot.Subscriptions.Count == subscribersCount)
        {
            _logger.LogError("New subscriber wasn't added");
            return;
        }

        var message = $"You are subscribed to events now. Remove me from the chat to unsubscribe.";
        _ = _bot.TrySendMessageAsync(subscriber, message, _logger);
    }

    void HandleBotIsRemovedFromChatAsync(ChatMemberUpdated chatMemberUpdate)
    {
        var subscriber = chatMemberUpdate.Chat.Id;
        _logger.LogDebug("Removing subscriber: {Subscriber}", subscriber);
        var subscribersCount = _bot.Subscriptions.Count;
        _bot.Subscriptions.Remove(subscriber);

        if (_bot.Subscriptions.Count == subscribersCount)
        {
            _logger.LogError("Subscriber wasn't removed");
        }
    }

    bool BotIsAddedToChat(ChatMemberUpdated chatMemberUpdate)
    {
        var newChatMember = chatMemberUpdate.NewChatMember;
        var oldChatMember = chatMemberUpdate.OldChatMember;
        // Doesn't match when the bot is being promoted.
        return newChatMember.User.Id == _bot.BotId && IsAddedToChat(newChatMember) && !IsAddedToChat(oldChatMember);
    }

    bool BotIsRemovedFromChat(ChatMemberUpdated chatMemberUpdate)
    {
        var newChatMember = chatMemberUpdate.NewChatMember;
        return newChatMember.User.Id == _bot.BotId && !IsAddedToChat(newChatMember);
    }

    static bool IsAddedToChat(ChatMember chatMember)
    {
        return chatMember.Status is not ChatMemberStatus.Left && chatMember.Status is not ChatMemberStatus.Kicked;
    }
}
