﻿using ReepoBot.Services.Telegram.UpdateHandlers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ReepoBot.Services.Telegram.UpdateTypeHandlers;

public class MyChatMemberTelegramUpdateHandler : ITelegramUpdateHandler
{
    readonly ILogger<MyChatMemberTelegramUpdateHandler> _logger;
    readonly TelegramBot _bot;

    public MyChatMemberTelegramUpdateHandler(ILoggerFactory loggerFactory, TelegramBot bot)
    {
        _logger = loggerFactory.CreateLogger<MyChatMemberTelegramUpdateHandler>();
        _bot = bot;
    }

    public async Task HandleAsync(Update update)
    {
        _logger.LogDebug("Dispatching MyChatMember update...");
        var chatMemberUpdate = update.MyChatMember!;
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
        _logger.LogDebug("No handler for {Update} is found", update.MyChatMember);
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
        try
        {
            await _bot.SendTextMessageAsync(
                subscriber,
                "You are subscribed to events now. Remove me from the chat to unsubscribe.");
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

    //static bool IsAddedToChat(ChatMemberUpdated chatMemberUpdated)
    //{
    //    var newChatMember = chatMemberUpdated.NewChatMember;
    //    var oldChatMember = chatMemberUpdated.OldChatMember;

    //    // Doesn't match when the bot is being promoted.
    //    return newChatMember.Status is not ChatMemberStatus.Left && newChatMember.Status is not ChatMemberStatus.Kicked
    //        && oldChatMember.Status is ChatMemberStatus.Left || oldChatMember.Status is ChatMemberStatus.Kicked;
    //}
}
