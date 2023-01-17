using Telegram.Bot.MessagePagination.CallbackQuery;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.MessagePagination;

internal static class MessagePaginatorControlButtons
{
    /// <inheritdoc cref="For(ChunkedText)"/>
    internal static InlineKeyboardMarkup? For(ChunkedMessage chunkedMessage) => For(chunkedMessage.Content);
    /// <remarks>
    /// Make sure to call it before <see cref="ChunkedText.NextChunk"/> but after <see cref="ChunkedText.MovePointerToBeginningOfPreviousChunk"/> to avoid logical errors.
    /// </remarks>
    /// <param name="chunkedText"></param>
    /// <returns>Paginator control buttons that depend on current position of <see cref="ChunkedText._pointer"/>.</returns>
    internal static InlineKeyboardMarkup? For(ChunkedText chunkedText) =>
        !chunkedText.IsChunked ? null :
        chunkedText.IsAtFirstChunk ? Next :
        chunkedText.IsAtLastChunk ? Previous :
        PreviousAndNext;

    internal static InlineKeyboardMarkup PreviousAndNext => new(new InlineKeyboardButton[]
    {
            InlineKeyboardButton.WithCallbackData("<", MessagePaginatorCallbackQueryFlags.Previous.ToString()),
            InlineKeyboardButton.WithCallbackData(">", MessagePaginatorCallbackQueryFlags.Next.ToString())
    });

    internal static InlineKeyboardMarkup Next => new(new InlineKeyboardButton[]
    { InlineKeyboardButton.WithCallbackData(">", MessagePaginatorCallbackQueryFlags.Next.ToString()) });

    internal static InlineKeyboardMarkup Previous => new(new InlineKeyboardButton[]
    { InlineKeyboardButton.WithCallbackData("<", MessagePaginatorCallbackQueryFlags.Previous.ToString()) });
}
