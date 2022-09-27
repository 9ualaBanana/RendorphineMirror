using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Telegram.MessageChunker.Models;

namespace Telegram.Telegram.MessageChunker.Services;

public class TextChunker
{
    readonly ChunkedMessagesAutoStorage _chunkedMessagesAutoStorage;


    public TextChunker(ChunkedMessagesAutoStorage chunkedMessagesAutoStorage)
    {
        _chunkedMessagesAutoStorage = chunkedMessagesAutoStorage;
    }


    public async Task SendChunkedMessageWith(Func<string, IReplyMarkup, Task<Message>> sender, string text)
    {
        var chunkedMessage = new ChunkedText(text);
        var telegramMessage = await sender(chunkedMessage.NextChunk(), ReplyMarkupFor(chunkedMessage));
        _chunkedMessagesAutoStorage.Add(new(telegramMessage, chunkedMessage));
    }

    public static InlineKeyboardMarkup ReplyMarkupFor(ChunkedText chunkedMessage)
    {
        var replyMarkup = InlineKeyboardMarkup.Empty();
        if (chunkedMessage.IsAtFirstChunk) replyMarkup.WithAddedButtonNext();
        else if (chunkedMessage.IsAtLastChunk) replyMarkup.WithAddedButtonPrevious();
        else { replyMarkup.WithAddedButtonNext(); replyMarkup.WithAddedButtonPrevious(); }

        return replyMarkup;
    }
}
