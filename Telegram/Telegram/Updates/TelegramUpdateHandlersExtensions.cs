﻿using Telegram.Bot.MessagePagination;
using Telegram.Bot.MessagePagination.CallbackQuery;
using Telegram.Services.Node;
using Telegram.Commands;
using Telegram.Telegram.Updates.Images;

namespace Telegram.Telegram.Updates;

public static class TelegramUpdateHandlersExtensions
{
    public static IServiceCollection AddTelegramUpdateHandlers(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<TelegramUpdateTypeHandler>()
            .AddScoped<TelegramMessageHandler>()
            .AddSingleton<MessagePaginator>()
            .AddScoped<MessagePaginatorCallbackQueryHandler>()
            .AddSingleton<ChunkedMessagesAutoStorage>()
            .AddTelegramBotCommands()
            .AddTelegramImageProcessing()
            .AddScoped<TelegramCallbackQueryHandler>()
            .AddScoped<TelegramChatMemberUpdatedHandler>()
            .AddSingleton<UserNodes>();
    }
}
