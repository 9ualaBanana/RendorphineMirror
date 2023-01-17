using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Telegram.Updates;

namespace Telegram.Bot.MessagePagination.CallbackQuery;

public class MessagePaginatorCallbackQueryHandler : TelegramCallbackQueryHandlerBase
{
    readonly ChunkedMessagesAutoStorage _chunkedMessagesAutoStorage;

    public MessagePaginatorCallbackQueryHandler(
        ILogger<TelegramCallbackQueryHandler> logger,
        TelegramBot bot,
        ChunkedMessagesAutoStorage chunkedMessagesAutoStorage) : base(logger, bot)
    {
        _chunkedMessagesAutoStorage = chunkedMessagesAutoStorage;
    }

    public override async Task HandleAsync(Update update)
    {
        var messagePaginatorCallbackData = new MessagePaginatorCallbackData(CallbackDataFrom(update));
        if (!_chunkedMessagesAutoStorage.TryGet(update.CallbackQuery!.Message!, out var paginatedMessage))
            return;

        if (messagePaginatorCallbackData.Value.HasFlag(MessagePaginatorCallbackQueryFlags.Previous))
            paginatedMessage.Content.MovePointerToBeginningOfPreviousChunk();
        var messagePaginatorControlButtons = MessagePaginatorControlButtons.For(paginatedMessage);
        await Bot.EditMessageTextAsync(ChatIdFrom(update), paginatedMessage.Message.MessageId, paginatedMessage.Content.NextChunk.Sanitize(), ParseMode.MarkdownV2);
        await Bot.EditMessageReplyMarkupAsync(ChatIdFrom(update), paginatedMessage.Message.MessageId, messagePaginatorControlButtons);
    }
}
