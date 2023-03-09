using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Telegram.Services.Node;

public class UserNodes : ConcurrentDictionary<string, NodeSupervisor>
{
    public bool TryGetUserNodeSupervisor(
        string userId,
        [MaybeNullWhen(false)] out NodeSupervisor userNodeSupervisor,
        TelegramBot bot,
        ChatId chatId)
    {
        if (TryGetValue(userId, out userNodeSupervisor)) return true;
        else
        {
            _ = bot.SendMessageAsync_(chatId, "No nodes owned by the current user were detected so far.");
            return false;
        }
    }
}
