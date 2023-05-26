using Telegram.Infrastructure.CallbackQueries;

namespace Telegram.Infrastructure.Bot.MessagePagination;

static class MessagePaginationExtensions
{
    internal static ITelegramBotBuilder AddMessagePagination(this ITelegramBotBuilder builder)
    {
        builder
            .AddCallbackQueryHandler<MessagePaginatorCallbackQueryHandler>()
            .AddCallbackQueries()

            .Services
            .AddSingleton<MessagePaginator>()
            .AddSingleton<ChunkedMessagesAutoStorage>()
            .AddSingleton<MessagePaginatorControlButtons>();
        return builder;
    }
}
