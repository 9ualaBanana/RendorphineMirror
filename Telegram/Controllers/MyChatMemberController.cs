using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Models;

namespace Telegram.Controllers;

[ApiController]
[Route($"telegram/{{token}}/{PathFragment}")]
public class MyChatMemberController : ControllerBase
{
    internal const string PathFragment = "my_chat_member";

    [HttpPost("removed")]
    public void Removed([FromServices] TelegramBot bot)
    {
        var subscriber = HttpContext.GetUpdate().MyChatMember!.Chat.Id;

        if (bot.Subscriptions.Remove(subscriber))
            bot.Logger.LogInformation("Subscriber was removed: {Subscriber}", subscriber);
    }
}

//async Task HandleBotIsAddedToChatAsync(ChatMemberUpdated chatMemberUpdate)
//{
//    var subscriber = chatMemberUpdate.Chat.Id;

//    var subscribersCount = Bot.Subscriptions.Count;
//    Bot.Subscriptions.Add(subscriber);

//    if (Bot.Subscriptions.Count == subscribersCount)
//    { Logger.LogError("New subscriber wasn't added"); return; }
//    else
//        Logger.LogInformation("New subscriber was added: {Subscriber}", subscriber);

//    const string message = "You are subscribed to events now. Remove me from the chat to unsubscribe.";
//    await Bot.TrySendMessageAsync(subscriber, message);
//}

//void HandleBotIsRemovedFromChat(ChatMemberUpdated chatMemberUpdate)
//{
//    var subscriber = chatMemberUpdate.Chat.Id;

//    if (Bot.Subscriptions.Remove(subscriber))
//        Logger.LogInformation("Subscriber was removed: {Subscriber}", subscriber);
//}

//bool BotIsAddedToChat(ChatMemberUpdated chatMemberUpdate)
//{
//    var newChatMember = chatMemberUpdate.NewChatMember;
//    var oldChatMember = chatMemberUpdate.OldChatMember;
//    // Doesn't match when the bot is being promoted.
//    return newChatMember.User.Id == Bot.BotId && IsAddedToChat(newChatMember) && !IsAddedToChat(oldChatMember);
//}

//bool BotIsRemovedFromChat(ChatMemberUpdated chatMemberUpdate)
//{
//    var newChatMember = chatMemberUpdate.NewChatMember;
//    return newChatMember.User.Id == Bot.BotId && !IsAddedToChat(newChatMember);
//}

//static bool IsAddedToChat(ChatMember chatMember)
//{
//    return chatMember.Status is not ChatMemberStatus.Left && chatMember.Status is not ChatMemberStatus.Kicked;
//}
