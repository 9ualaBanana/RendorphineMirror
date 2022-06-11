using ReepoBot.Models;
using ReepoBot.Services.Node;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramHelper;

namespace ReepoBot.Services.Telegram;

public class TelegramUpdateHandler
{
    readonly ILoggerFactory _loggerFactory;
    readonly ILogger _logger;
    readonly TelegramBot _bot;
    readonly NodeSupervisor _nodeSupervisor;

    public TelegramUpdateHandler(
        ILoggerFactory loggerFactory,
        TelegramBot bot,
        NodeSupervisor nodeSupervisor)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<TelegramUpdateHandler>();
        _nodeSupervisor = nodeSupervisor;
        _bot = bot;
    }

    public async Task HandleAsync(Update update)
    {
        _logger.LogDebug("Dispatching update type...");
        switch (update.Type)
        {
            case UpdateType.Message:
                await HandleMessageUpdate(update);
                break;
            case UpdateType.MyChatMember:
                await HandleChatMember(update.MyChatMember!);
                break;
            default:
                _logger.LogWarning("Update with {Type} type couldn't be handled", update.Type);
                break;
        }
    }

    async Task HandleMessageUpdate(Update update)
    {
        var message = update.Message!;
        _logger.LogDebug("Dispatching text message...");
        if (IsCommand(message))
        {
            await HandleCommandUpdate(update);
            return;
        }
        if (IsSystem(message))
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

    bool IsSystem(Message message)
    {
        return message.LeftChatMember?.Id == _bot.BotId || message.NewChatMembers?.First().Id == _bot.BotId;
    }

    async Task HandleCommandUpdate(Update update)
    {
        var command = update.Message!.Text![1..];
        _logger.LogDebug("Dispatching {Command} bot command...", command);
        if (command.StartsWith("pinglist"))
        {
            await HandlePingList(update);
            return;
        }
        if (command.StartsWith("ping"))
        {
            await HandlePing(update);
            return;
        }
        if (command.StartsWith("online"))
        {
            await HandleOnline(update);
            return;
        }
        if (command.StartsWith("offline"))
        {
            await HandleOffline(update);
            return;
        }
        if (command.StartsWith("remove"))
        {
            await HandleRemove(update);
            return;
        }
        _logger.LogWarning("No handler for {Command} command is found", command);
    }

    async Task HandlePingList(Update update)
    {
        _logger.LogDebug("Listing all nodes...");
        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("*All Nodes*");
        messageBuilder.AppendLine("------------------------------------------------");
        foreach (var nodeInfo in _nodeSupervisor.AllNodes.OrderBy(node => node.PCName))
        {
            messageBuilder.Append(nodeInfo.BriefInfoMDv2);
            if (_nodeSupervisor.NodesOffline.Contains(nodeInfo)) messageBuilder.AppendLine(" - *OFFLINE*");
            else messageBuilder.AppendLine();
        }
        var message = messageBuilder.ToString().Sanitize();

        try
        {
            await _bot.SendTextMessageAsync(
                update.Message!.Chat.Id,
                message,
                parseMode: ParseMode.MarkdownV2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ping message wasn't sent.");
        }
    }

    async Task HandlePing(Update update)
    {
        var splitCommand = update.Message!.Text!.Split();
        _logger.LogDebug("Building the message with online nodes...");

        IEnumerable<KeyValuePair<NodeInfo, TimerPlus>> nodesOnlineToList;

        if (splitCommand.Length > 1)
        {
            var lcNodeNamesToList = splitCommand[1..]
                .Select(nodeName => nodeName.ToLower())
                .ToHashSet();

            nodesOnlineToList = _nodeSupervisor.NodesOnline.Where(
                nodeInfo => lcNodeNamesToList.Any(
                    lcNodeNameToList => nodeInfo.Key.PCName.ToLower().StartsWith(lcNodeNameToList)
                    )
                );
        }
        else
        {
            nodesOnlineToList = _nodeSupervisor.NodesOnline;
        }

        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("*Node* | *Uptime*");
        messageBuilder.AppendLine("------------------------------------------------");
        foreach (var (nodeInfo, _) in nodesOnlineToList.OrderBy(nodeOnline => nodeOnline.Key.PCName))
        {
            var uptime = _nodeSupervisor.GetUptimeFor(nodeInfo);
            // It could already go offline.
            if (uptime is null) continue;

            var escapedUptime = $"{uptime:d\\.hh\\:mm}";
            messageBuilder.AppendLine($"{nodeInfo.BriefInfoMDv2} | {escapedUptime}");
        }
        var message = messageBuilder.ToString().Sanitize();

        try
        {
            await _bot.SendTextMessageAsync(
                update.Message!.Chat.Id,
                message,
                parseMode: ParseMode.MarkdownV2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong when trying to send the message\n{Message}", message);
        }
    }

    async Task HandleOnline(Update update)
    {
        var message = $"Online: *{_nodeSupervisor.NodesOnline.Count}*\nOffline: {_nodeSupervisor.NodesOffline.Count}".Sanitize();
        try
        {
            await _bot.SendTextMessageAsync(
                update.Message!.Chat.Id,
                message,
                parseMode: ParseMode.MarkdownV2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Message wasn't sent.");
        }
    }

    async Task HandleOffline(Update update)
    {
        _logger.LogDebug("Listing offline nodes...");
        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("*Offline Nodes*");
        messageBuilder.AppendLine("------------------------------------------------");
        foreach (var nodeInfo in _nodeSupervisor.NodesOffline.OrderBy(node => node.PCName))
        {
            messageBuilder.AppendLine(nodeInfo.BriefInfoMDv2);
        }
        var message = messageBuilder.ToString().Sanitize();

        try
        {
            await _bot.SendTextMessageAsync(
                update.Message!.Chat.Id,
                message,
                parseMode: ParseMode.MarkdownV2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Message wasn't sent.");
        }
    }

    async Task HandleRemove(Update update)
    {
        var nodePCNames = update.Message!.Text!
            .Split()[1..]
            .Select(nodePCName => nodePCName.ToLower())
            .ToArray();

        int nodesRemoved = _nodeSupervisor.TryRemoveNodesWithNames(nodePCNames);


        if (nodesRemoved == 0)
        {
            var unsuccessfulMessage = $"Nodes with specified names are either online or not found.".Sanitize();
            try
            {
                await _bot.SendTextMessageAsync(
                    update.Message!.Chat.Id,
                    unsuccessfulMessage,
                    parseMode: ParseMode.MarkdownV2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Message wasn't sent.");
            }
            return;
        }

        var message = $"{nodesRemoved} out of {nodePCNames.Length} were removed.".Sanitize();
        try
        {
            await _bot.SendTextMessageAsync(
                update.Message!.Chat.Id,
                message,
                parseMode: ParseMode.MarkdownV2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Message wasn't sent.");
        }
    }

    async Task HandleChatMember(ChatMemberUpdated chatMemberUpdate)
    {
        _logger.LogDebug("Dispatching MyChatMember update...");
        if (BotIsAddedToChat(chatMemberUpdate))
        {
            await HandleBotIsAddedToChatAsync(chatMemberUpdate);
            return;
        }
        if (BotIsRemovedFromChat(chatMemberUpdate))
        {
            await HandleBotIsRemovedFromChatAsync(chatMemberUpdate);
            return;
        }
        _logger.LogDebug("No handler for {Update} is found", chatMemberUpdate);
    }

    async Task HandleBotIsAddedToChatAsync(ChatMemberUpdated chatMemberUpdate)
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

        var message = $"You are subscribed to events now. Remove me from the chat to unsubscribe.".Sanitize();
        try
        {
            await _bot.SendTextMessageAsync(
                subscriber,
                message,
                parseMode: ParseMode.MarkdownV2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Subscription notification wasn't sent");
        }
    }

    Task HandleBotIsRemovedFromChatAsync(ChatMemberUpdated chatMemberUpdate)
    {
        var subscriber = chatMemberUpdate.Chat.Id;
        _logger.LogDebug("Removing subscriber: {Subscriber}", subscriber);
        var subscribersCount = _bot.Subscriptions.Count;
        _bot.Subscriptions.Remove(subscriber);

        if (_bot.Subscriptions.Count == subscribersCount)
        {
            _logger.LogError("Subscriber wasn't removed");
        }
        return Task.CompletedTask;
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
