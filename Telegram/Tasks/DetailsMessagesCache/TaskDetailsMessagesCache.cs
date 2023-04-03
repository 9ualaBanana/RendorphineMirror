using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Telegram.Bot.Types;

namespace Telegram.Tasks.DetailsMessagesCache;

public class TaskDetailsMessagesCache
{
    readonly Dictionary<UniqueMessage, AutoStorage<Message>> _cache = new();

    internal void Add(Message callbackQuerySource, Message details)
    {
        var key = UniqueMessage.From(callbackQuerySource);
        _cache.TryAdd(key, new(new MessageIdEqualityComparer(), (StorageTime)TimeSpan.FromMinutes(15)));
        _cache[key].Add(details);
    }

    internal Message? TryRetrieveBy(Message callbackQuerySource)
        // Likely auto storage is needed only to support auto removal, cachedMessages will always contain only one element, right?
        => _cache.TryGetValue(UniqueMessage.From(callbackQuerySource), out var cachedMessages) ?
        cachedMessages.SingleOrDefault() : null;


    class MessageIdEqualityComparer : IEqualityComparer<AutoStorageItem<Message>>
    {
        public bool Equals(AutoStorageItem<Message>? x, AutoStorageItem<Message>? y)
            => x?.Value.MessageId == y?.Value.MessageId;

        public int GetHashCode([DisallowNull] AutoStorageItem<Message> obj)
            => obj.Value.MessageId.GetHashCode();
    }
}
