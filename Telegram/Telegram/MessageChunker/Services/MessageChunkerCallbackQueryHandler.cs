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

        var newMessageContent = messageChunkerCallbackData.Value.HasFlag(MessageChunkerCallbackQueryFlags.Previous) ? messageToEdit.ChunkedText.PreviousChunk() : messageToEdit.ChunkedText.NextChunk();
        await Bot.EditMessageTextAsync(update.CallbackQuery.Message.Chat.Id, messageToEdit.Message.MessageId, newMessageContent, ParseMode.MarkdownV2);
        await Bot.EditMessageReplyMarkupAsync(update.CallbackQuery.Message.Chat.Id, messageToEdit.Message.MessageId, TelegramMessageChunker.ReplyMarkupFor(messageToEdit.ChunkedText));
    }
}
