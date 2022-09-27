using System.Collections.Specialized;
using Telegram.Telegram.MessageChunker.Models;

namespace Telegram.Telegram.MessageChunker.Services;

public class ChunkedMessagesAutoStorage
{
    readonly Dictionary<int, ChunkedMessage> _messageIdMappedToChunkedMessages;
    readonly AutoStorage<ChunkedMessage> _validMessages;


    public ChunkedMessagesAutoStorage()
    {
        _messageIdMappedToChunkedMessages = new();
        _validMessages = new(defaultStorageTime: TimeSpan.FromMinutes(30));
        _validMessages.ItemStorageTimeElapsed += (s, e) => _messageIdMappedToChunkedMessages.Remove(e.Value.Message.MessageId);
    }


    public void Add(ChunkedMessage chunkedTelegramMessage)
    {
        _validMessages.Add(chunkedTelegramMessage);
        _messageIdMappedToChunkedMessages.Add(chunkedTelegramMessage.Message.MessageId, chunkedTelegramMessage);
    }

    public ChunkedMessage? this[int messageId]
    {
        get
        {
            if (_messageIdMappedToChunkedMessages.TryGetValue(messageId, out var message))
            { _validMessages.TryResetStorageTime(message); return message; }
            else return null;
        }
    }
}
