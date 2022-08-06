﻿using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ReepoBot.Services.Telegram.Updates;

public class TelegramChatMemberUpdatedHandler
{
    readonly ILogger _logger;

    readonly TelegramBot _bot;

    public TelegramChatMemberUpdatedHandler(
        ILogger<TelegramChatMemberUpdatedHandler> logger,
        TelegramBot bot)
    {
        _logger = logger;
        _bot = bot;
    }

    public void Handle(Update update)
    {
        _logger.LogDebug("Dispatching {ChatMemberUpdated}...", nameof(ChatMemberUpdated));

        var chatMemberUpdate = update.MyChatMember!;
        if (BotIsAddedToChat(chatMemberUpdate))
            { HandleBotIsAddedToChat(chatMemberUpdate); return; }
        else if (BotIsRemovedFromChat(chatMemberUpdate))
            { HandleBotIsRemovedFromChat(chatMemberUpdate); return; }
    }

    void HandleBotIsAddedToChat(ChatMemberUpdated chatMemberUpdate)
    {
        var subscriber = chatMemberUpdate.Chat.Id;

        var subscribersCount = _bot.Subscriptions.Count;
        _bot.Subscriptions.Add(subscriber);

        if (_bot.Subscriptions.Count == subscribersCount)
            { _logger.LogError("New subscriber wasn't added"); return; }
        else
            _logger.LogInformation("New subscriber was added: {Subscriber}", subscriber);

        const string message = "You are subscribed to events now. Remove me from the chat to unsubscribe.";
        _ = _bot.TrySendMessageAsync(subscriber, message, _logger);
    }

    void HandleBotIsRemovedFromChat(ChatMemberUpdated chatMemberUpdate)
    {
        var subscriber = chatMemberUpdate.Chat.Id;
        var subscribersCount = _bot.Subscriptions.Count;
        _bot.Subscriptions.Remove(subscriber);

        if (_bot.Subscriptions.Count == subscribersCount)
            _logger.LogError("Subscriber wasn't removed");
        else
            _logger.LogInformation("Subscriber was removed: {Subscriber}", subscriber);
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