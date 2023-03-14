using NodeCommon.Tasks;
using Telegram.Bot;

namespace Telegram.Infrastructure.Tasks;

public class OwnedRegisteredTasksCache
{
    readonly Dictionary<TypedRegisteredTask, TelegramBotUser> _ownedRegisteredTasksCache = new(new TypedRegisteredTask.IdEqualityComparer());

    internal void Add(OwnedRegisteredTask ownedRegisteredTask)
        => _ownedRegisteredTasksCache.Add(ownedRegisteredTask.Task, ownedRegisteredTask.Owner);

    internal OwnedRegisteredTask Retrieve(TypedRegisteredTask registeredTask)
        => new(registeredTask, _ownedRegisteredTasksCache[registeredTask]);
}
