using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Controllers;
using Telegram.Models;

namespace Telegram.Middleware.UpdateRouting.UpdateTypeRouting;

public class MyChatMemberRouterMiddleware : IUpdateTypeRouter
{
    readonly ILogger _logger;

    readonly TelegramBot _bot;

    public MyChatMemberRouterMiddleware(TelegramBot bot, ILogger<MyChatMemberRouterMiddleware> logger)
    {
        _bot = bot;
        _logger = logger;
    }

    public bool Matches(Update update) => update.Type is UpdateType.MyChatMember;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var relativePath = new StringBuilder($"/{MyChatMemberController.PathFragment}");

        var chatMemberUpdate = UpdateContext.RetrieveFromCacheOf(context).Update.MyChatMember!;

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
