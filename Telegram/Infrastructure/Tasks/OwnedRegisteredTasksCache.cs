using Telegram.Bot;

namespace Telegram.Infrastructure.Tasks;

public class OwnedRegisteredTasksCache
{
    readonly Dictionary<RegisteredTask, TelegramBotUser> _ownedRegisteredTasksCache = new();

    internal void Add(OwnedRegisteredTask ownedRegisteredTask)
        => _ownedRegisteredTasksCache.Add(ownedRegisteredTask.Task, ownedRegisteredTask.Owner);

    internal OwnedRegisteredTask Retrieve(RegisteredTask registeredTask)
        => new(registeredTask, _ownedRegisteredTasksCache[registeredTask]);
}
