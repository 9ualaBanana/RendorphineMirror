using NLog;
using ILogger = NLog.ILogger;

namespace Telegram.Tasks;

/// <summary>
/// Cache for storing instances of <see cref="TypedRegisteredTask"/> with their <see cref="OwnedRegisteredTask.Owner"/>
/// represented as <see cref="TelegramBot.User"/>.
/// </summary>
public class OwnedRegisteredTasksCache
{
    readonly Dictionary<TypedRegisteredTask, TelegramBot.User> _ownedRegisteredTasksCache
        = new(new RegisteredTask.IdEqualityComparer());

    readonly static ILogger _logger = LogManager.GetCurrentClassLogger();

    internal void Add(OwnedRegisteredTask ownedRegisteredTask)
    {
        _ownedRegisteredTasksCache.Add(ownedRegisteredTask, ownedRegisteredTask.Owner);
        _logger.Trace("{Task} is added to the cache.", ownedRegisteredTask);
    }

    internal OwnedRegisteredTask Retrieve(TypedRegisteredTask registeredTask)
    {
        if (_ownedRegisteredTasksCache.Remove(registeredTask, out var taskOwner))
            return registeredTask.OwnedBy(taskOwner);
        else
        {
            var exception = new ArgumentException($"{registeredTask} is not stored inside the cache.", nameof(registeredTask));
            _logger.Error("{Task} couldn't be retrieved from the cache.");
            throw exception;
        }
    }
}
