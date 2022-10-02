using NLog;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Telegram.MessageChunker.Models;

namespace Telegram.Telegram.MessageChunker.Services;

public class TelegramMessageChunker
{
    readonly Microsoft.Extensions.Logging.ILogger _logger;

    readonly ChunkedMessagesAutoStorage _chunkedMessagesAutoStorage;


    public TelegramMessageChunker(
        ILogger<TelegramMessageChunker> logger,
        ChunkedMessagesAutoStorage chunkedMessagesAutoStorage)
    {
        _logger = logger;
        _chunkedMessagesAutoStorage = chunkedMessagesAutoStorage;
    }


    public async Task<Message?> TrySendChunkedMessageAsync(TelegramBot bot, ChatId chatId, string text)
    {
        var chunkedText = new ChunkedText(text);
        var replyMarkup = ReplyMarkupFor(chunkedText);

        var telegramMessage = (await bot.TrySendMessageAsyncCore(chatId, chunkedText.NextChunk, replyMarkup))!;
        if (telegramMessage is not null && chunkedText.IsChunked)
            _chunkedMessagesAutoStorage.Add(new(telegramMessage, chunkedText));
        return telegramMessage;
    }

    public static InlineKeyboardMarkup? ReplyMarkupFor(ChunkedText chunkedText)
    {
        if (!chunkedText.IsChunked) return null;

        if (chunkedText.IsAtFirstChunk) return TelegramMessageChunkerInlineKeyboardMarkup.WithButtonNext;
        else if (chunkedText.IsAtLastChunk) return TelegramMessageChunkerInlineKeyboardMarkup.WithButtonPrevious;
        else return TelegramMessageChunkerInlineKeyboardMarkup.WithButtonsNextAndPrevious;
    }
}
