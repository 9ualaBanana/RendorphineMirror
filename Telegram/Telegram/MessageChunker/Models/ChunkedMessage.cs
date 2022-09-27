using Telegram.Bot.Types;

namespace Telegram.Telegram.MessageChunker.Models;

public record ChunkedMessage(Message Message, ChunkedText ChunkedText)
{
    public ChunkedMessage(Message message)
        : this(message, new(message.Text!))
    {
    }
}
