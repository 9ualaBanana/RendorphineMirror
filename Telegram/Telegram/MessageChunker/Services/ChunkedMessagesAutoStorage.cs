using System.Collections.Specialized;
using Telegram.Telegram.MessageChunker.Models;

namespace Telegram.Telegram.MessageChunker.Services;

public class ChunkedMessagesAutoStorage
{
    readonly Dictionary<int, ChunkedMessage> _messageIdsMappedToChunkedMessages;
    readonly AutoStorage<ChunkedMessage> _validMessages;


    public ChunkedMessagesAutoStorage()
    {
        _messageIdsMappedToChunkedMessages = new();
        _validMessages = new(defaultStorageTime: TimeSpan.FromMinutes(30));
        _validMessages.ItemStorageTimeElapsed += (_, e) => _messageIdsMappedToChunkedMessages.Remove(e.Value.Message.MessageId);
    }


    public void Add(ChunkedMessage chunkedTelegramMessage)
    {
        _validMessages.Add(chunkedTelegramMessage);
        _messageIdsMappedToChunkedMessages.Add(chunkedTelegramMessage.Message.MessageId, chunkedTelegramMessage);
    }

    public ChunkedMessage? this[int messageId]
    {
        get
        {
            if (_messageIdsMappedToChunkedMessages.TryGetValue(messageId, out var message))
            { _validMessages.TryResetStorageTime(message); return message; }
            else return null;
        }
    }
}
