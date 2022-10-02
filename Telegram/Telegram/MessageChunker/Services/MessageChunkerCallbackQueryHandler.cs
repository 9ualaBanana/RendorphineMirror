using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Telegram.MessageChunker.Models;
using Telegram.Telegram.Updates;

namespace Telegram.Telegram.MessageChunker.Services;

public class MessageChunkerCallbackQueryHandler : TelegramCallbackQueryHandlerBase
{
    readonly ChunkedMessagesAutoStorage _chunkedMessagesAutoStorage;


    public MessageChunkerCallbackQueryHandler(
        ILogger<TelegramCallbackQueryHandler> logger,
        TelegramBot bot,
        ChunkedMessagesAutoStorage chunkedMessagesAutoStorage) : base(logger, bot)
    {
        _chunkedMessagesAutoStorage = chunkedMessagesAutoStorage;
    }


    public override async Task HandleAsync(Update update)
    {
        var messageChunkerCallbackData = new MessageChunkerCallbackData(CallbackDataFrom(update));
        var messageToEdit = _chunkedMessagesAutoStorage[update.CallbackQuery!.Message!.MessageId];
        if (messageToEdit is null) return;

        if (messageChunkerCallbackData.Value.HasFlag(MessageChunkerCallbackQueryFlags.Previous)) messageToEdit.ChunkedText.ToPreviousChunk();
        var replyMarkup = TelegramMessageChunker.ReplyMarkupFor(messageToEdit.ChunkedText);
        await Bot.EditMessageTextAsync(update.CallbackQuery.Message.Chat.Id, messageToEdit.Message.MessageId, messageToEdit.ChunkedText.NextChunk.Sanitize(), ParseMode.MarkdownV2);
        await Bot.EditMessageReplyMarkupAsync(update.CallbackQuery.Message.Chat.Id, messageToEdit.Message.MessageId, replyMarkup);
    }
}
