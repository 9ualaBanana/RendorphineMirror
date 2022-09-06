using System.Collections.Concurrent;
using Telegram.Bot.Types;

namespace Telegram.Services.Telegram.Authentication;

public class AuthentcatedUsersRegistry : ConcurrentDictionary<string, HashSet<ChatId>>
{
}
