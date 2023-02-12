using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Telegram.Authentication.Models;

namespace Telegram.Services.Node;

public class UserNodes : ConcurrentDictionary<string, NodeSupervisor>
{
    public bool TryGetUserNodeSupervisor(
        ChatAuthenticationToken authentictationToken,
        [MaybeNullWhen(false)] out NodeSupervisor userNodeSupervisor,
        TelegramBot bot,
        ChatId chatId)
    {
        if (TryGetValue(authentictationToken.MPlus.UserId, out userNodeSupervisor)) return true;
        else
        {
            _ = bot.SendMessageAsync_(chatId, "No nodes owned by the current user were detected so far.");
            return false;
        }
    }
}
