using Telegram.Bot.Types.ReplyMarkups;
using Telegram.CallbackQueries;

namespace Telegram.Bot.MessagePagination;

public class MessagePaginatorControlButtons
{
    readonly CallbackQuerySerializer _serializer;

    public MessagePaginatorControlButtons(CallbackQuerySerializer serializer)
    {
        _serializer = serializer;
    }

    /// <inheritdoc cref="For(ChunkedText)"/>
    internal InlineKeyboardMarkup? For(ChunkedMessage chunkedMessage) => For(chunkedMessage.Content);
    /// <remarks>
    /// Make sure to call it before <see cref="ChunkedText.NextChunk"/> but after <see cref="ChunkedText.MovePointerToBeginningOfPreviousChunk"/> to avoid logical errors.
    /// </remarks>
    /// <param name="chunkedText"></param>
    /// <returns>Paginator control buttons that depend on current position of <see cref="ChunkedText._pointer"/>.</returns>
    internal InlineKeyboardMarkup? For(ChunkedText chunkedText) =>
        !chunkedText.IsChunked ? null :
        chunkedText.IsAtFirstChunk ? Next :
        chunkedText.IsAtLastChunk ? Previous :
        PreviousAndNext;

    InlineKeyboardMarkup PreviousAndNext => new(new InlineKeyboardButton[] { Previous, Next });

    internal InlineKeyboardButton Previous
        => InlineKeyboardButton.WithCallbackData("<",
            _serializer.Serialize(new MessagePaginatorCallbackQuery.Builder<MessagePaginatorCallbackQuery>()
                .Data(MessagePaginatorCallbackData.Previous)
                .Build())
            );

    internal InlineKeyboardButton Next
        => InlineKeyboardButton.WithCallbackData(">",
            _serializer.Serialize(new MessagePaginatorCallbackQuery.Builder<MessagePaginatorCallbackQuery>()
                .Data(MessagePaginatorCallbackData.Next)
                .Build())
            );
}
