using NLog;
using Telegram.Infrastructure.Bot;
using ILogger = NLog.ILogger;

namespace Telegram.Infrastructure.Tasks;

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
        => _ownedRegisteredTasksCache.Add(ownedRegisteredTask._, ownedRegisteredTask.Owner);

    internal OwnedRegisteredTask Retrieve(TypedRegisteredTask registeredTask)
    {
        if (_ownedRegisteredTasksCache.Remove(registeredTask, out var taskOwner))
            return new(registeredTask, taskOwner);
        else
        {
            var exception = new ArgumentException($"{nameof(registeredTask)} is not stored inside this {nameof(OwnedRegisteredTasksCache)}.");
            _logger.Error(exception);
            throw exception;
        }
    }
}
