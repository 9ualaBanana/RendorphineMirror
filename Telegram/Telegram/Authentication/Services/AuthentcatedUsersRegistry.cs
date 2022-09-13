using System.Collections.Concurrent;
using Telegram.Bot.Types;

namespace Telegram.Telegram.Authentication.Services;

public class AuthentcatedUsersRegistry : ConcurrentDictionary<string, HashSet<ChatId>>
{
}
