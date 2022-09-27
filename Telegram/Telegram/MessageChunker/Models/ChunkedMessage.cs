using Telegram.Bot.Types;

namespace Telegram.Telegram.MessageChunker.Models;

public record ChunkedMessage(Message Message, ChunkedText ChunkedText)
{
    public ChunkedMessage(Message telegramMessage)
        : this(telegramMessage, new(telegramMessage.Text!))
    {
    }
}
