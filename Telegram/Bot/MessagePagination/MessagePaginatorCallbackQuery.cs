using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Infrastructure.CallbackQueries;

namespace Telegram.Bot.MessagePagination;

public class MessagePaginatorCallbackQueryHandler : CallbackQueryHandler<MessagePaginatorCallbackQuery, MessagePaginatorCallbackData>
{
    readonly ChunkedMessagesAutoStorage _chunkedMessagesAutoStorage;
    readonly MessagePaginatorControlButtons _controlButtons;

    public MessagePaginatorCallbackQueryHandler(
        ChunkedMessagesAutoStorage chunkedMessagesAutoStorage,
        MessagePaginatorControlButtons controlButtons,
        CallbackQuerySerializer serializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<MessagePaginatorCallbackQueryHandler> logger)
        : base(serializer, bot, httpContextAccessor, logger)
    {
        _chunkedMessagesAutoStorage = chunkedMessagesAutoStorage;
        _controlButtons = controlButtons;
    }

    public override async Task HandleAsync(MessagePaginatorCallbackQuery callbackQuery, HttpContext context)
    {
        if (!_chunkedMessagesAutoStorage.TryGet(Update.CallbackQuery!.Message!, out var paginatedMessage))
            return;

        if (callbackQuery.Data is MessagePaginatorCallbackData.Previous)
            paginatedMessage.Content.MovePointerToBeginningOfPreviousChunk();
        var messagePaginatorControlButtons = _controlButtons.For(paginatedMessage);
        await Bot.EditMessageTextAsync(ChatId, paginatedMessage.Message.MessageId, paginatedMessage.Content.NextChunk.Sanitize(), ParseMode.MarkdownV2);
        await Bot.EditMessageReplyMarkupAsync(ChatId, paginatedMessage.Message.MessageId, messagePaginatorControlButtons);
    }
}

public record MessagePaginatorCallbackQuery : CallbackQuery<MessagePaginatorCallbackData>
{
}

public enum MessagePaginatorCallbackData
{
    Previous,
    Next
}
