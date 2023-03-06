﻿using Telegram.Bot.Types;

namespace Telegram.Bot.MessagePagination;

public class MessagePaginator
{
    internal const int MessageLengthLimit = 4096;

    readonly ChunkedMessagesAutoStorage _chunkedMessagesAutoStorage;

    public MessagePaginator(ChunkedMessagesAutoStorage chunkedMessagesAutoStorage)
    {
        _chunkedMessagesAutoStorage = chunkedMessagesAutoStorage;
    }

    internal static bool MustBeUsedToSend(string text) => text.Length > MessageLengthLimit;

    internal async Task<Message> SendPaginatedMessageAsyncUsing(
        TelegramBot bot,
        ChatId chatId,
        string text,
        bool? disableWebPagePreview = default,
        bool? disableNotification = default,
        bool? protectContent = default,
        CancellationToken cancellationToken = default)
    {
        var chunkedText = new ChunkedText(text);
        var messagePaginatorControlButtons = MessagePaginatorControlButtons.For(chunkedText);

        var message = await bot.SendMessageAsyncCore(chatId, chunkedText.NextChunk, messagePaginatorControlButtons, disableWebPagePreview, disableNotification, protectContent, cancellationToken);
        if (chunkedText.IsChunked)
            _chunkedMessagesAutoStorage.Add(new(message, chunkedText));
        return message;
    }
}