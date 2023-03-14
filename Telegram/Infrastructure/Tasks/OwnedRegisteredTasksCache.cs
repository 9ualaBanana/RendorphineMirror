using Telegram.Bot;

namespace Telegram.Infrastructure.Tasks;

public class OwnedRegisteredTasksCache
{
    readonly Dictionary<RegisteredTypedTask, TelegramBotUser> _ownedRegisteredTasksCache = new();

    internal void Add(OwnedRegisteredTask ownedRegisteredTask)
        => _ownedRegisteredTasksCache.Add(ownedRegisteredTask.Task, ownedRegisteredTask.Owner);

    internal OwnedRegisteredTask Retrieve(RegisteredTypedTask registeredTask)
        => new(registeredTask, _ownedRegisteredTasksCache[registeredTask]);
}
