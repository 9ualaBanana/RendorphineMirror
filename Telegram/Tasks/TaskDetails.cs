using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Telegram.Tasks;

public class TaskDetails
{
    readonly TelegramBot _bot;
    readonly CachedMessages _sentTaskDetailsMessages;

    public TaskDetails(TelegramBot bot, CachedMessages sentTaskDetailsMessages)
    {
        _bot = bot;
        _sentTaskDetailsMessages = sentTaskDetailsMessages;
    }

    internal async Task<Message> SendAsync(ChatId chatId, string details, Message callbackQuerySource, CancellationToken cancellationToken)
    {
        if (_sentTaskDetailsMessages.ProducedBy(callbackQuerySource) is Message sentTaskDetailsMessage)
            return await _bot.EditMessageAsync_(chatId, sentTaskDetailsMessage.MessageId, details, cancellationToken: cancellationToken);
        else
        {
            Message taskDetails = await _bot.SendMessageAsync_(chatId, details, cancellationToken: cancellationToken);
            _sentTaskDetailsMessages.Add(callbackQuerySource, taskDetails);
            return taskDetails;
        }
    }


    public class CachedMessages
    {
        readonly IMemoryCache _cache;

        public CachedMessages(IMemoryCache cache)
        {
            _cache = cache;
        }

        internal void Add(Message callbackQuerySource, Message details)
            => _cache.CreateEntry(UniqueMessage.From(callbackQuerySource))
            .SetValue(details)
            .SetSlidingExpiration(TimeSpan.FromMinutes(10))
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(60))
            .Dispose();

        internal Message? ProducedBy(Message callbackQuerySource)
            => _cache.TryGetValue(UniqueMessage.From(callbackQuerySource), out Message details) ?
            details : null;
    }
}
