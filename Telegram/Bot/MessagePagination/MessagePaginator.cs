using Telegram.Bot.Types;

namespace Telegram.Bot.MessagePagination;

public class MessagePaginator
{
    internal const int MessageLengthLimit = 4096;

    readonly ChunkedMessagesAutoStorage _chunkedMessagesAutoStorage;

    public MessagePaginator(ChunkedMessagesAutoStorage chunkedMessagesAutoStorage)
    {
        _chunkedMessagesAutoStorage = chunkedMessagesAutoStorage;
    }

    internal async Task<Message?> TrySendPaginatedMessageAsync(TelegramBot bot, ChatId chatId, string text)
    {
        var chunkedText = new ChunkedText(text);
        var messagePaginatorControlButtons = MessagePaginatorControlButtons.For(chunkedText);

        var message = (await bot.TrySendMessageAsyncCore(chatId, chunkedText.NextChunk, messagePaginatorControlButtons))!;
        if (message is not null && chunkedText.IsChunked)
            _chunkedMessagesAutoStorage.Add(new(message, chunkedText));
        return message;
    }

    internal static bool MustBeUsedToSend(string text) => text.Length > MessageLengthLimit;
}
