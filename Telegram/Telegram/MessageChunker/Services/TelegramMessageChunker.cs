using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Telegram.MessageChunker.Models;

namespace Telegram.Telegram.MessageChunker.Services;

public class TelegramMessageChunker
{
    readonly ChunkedMessagesAutoStorage _chunkedMessagesAutoStorage;


    public TelegramMessageChunker(ChunkedMessagesAutoStorage chunkedMessagesAutoStorage)
    {
        _chunkedMessagesAutoStorage = chunkedMessagesAutoStorage;
    }


    public async Task<Message> TrySendChunkedMessageAsync(TelegramBot bot, ChatId chatId, string text)
    {
        var chunkedText = new ChunkedText(text);
        var telegramMessage = (await bot.TrySendMessageAsyncCore(chatId, chunkedText.NextChunk(), ReplyMarkupFor(chunkedText)))!;
        _chunkedMessagesAutoStorage.Add(new(telegramMessage, chunkedText));
        return telegramMessage;
    }

    public static InlineKeyboardMarkup? ReplyMarkupFor(ChunkedText chunkedText)
    {
        if (!chunkedText.IsChunked) return null;

        var replyMarkup = InlineKeyboardMarkup.Empty();
        if (chunkedText.IsAtFirstChunk) replyMarkup.WithAddedButtonNext();
        else if (chunkedText.IsAtLastChunk) replyMarkup.WithAddedButtonPrevious();
        else { replyMarkup.WithAddedButtonNext(); replyMarkup.WithAddedButtonPrevious(); }

        return replyMarkup;
    }
}
