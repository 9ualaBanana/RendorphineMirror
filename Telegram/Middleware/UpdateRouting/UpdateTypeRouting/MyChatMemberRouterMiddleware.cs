using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Controllers;
using Telegram.Models;

namespace Telegram.Middleware.UpdateRouting.UpdateTypeRouting;

public class MyChatMemberRouterMiddleware : IUpdateTypeRouter
{
    readonly TelegramBot _bot;

    public MyChatMemberRouterMiddleware(TelegramBot bot)
    {
        _bot = bot;
    }

    public bool Matches(Update update) => update.Type is UpdateType.MyChatMember;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var relativePath = new StringBuilder('/'+MyChatMemberController.PathFragment);

        var chatMemberUpdate = context.GetUpdate().MyChatMember!;

        if (BotIsRemovedFromChat(chatMemberUpdate))
            relativePath.Append("/removed");

        context.Request.Path += relativePath.ToString();
        await next(context);
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
