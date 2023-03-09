using Telegram.CallbackQueries;

namespace Telegram.Bot.MessagePagination;

internal static class MessagePaginationExtensions
{
    internal static IServiceCollection AddMessagePagination(this IServiceCollection services)
        => services
        .AddSingleton<ChunkedMessagesAutoStorage>()
        .AddCallbackQueries()
        .AddSingleton<MessagePaginatorControlButtons>()
        .AddSingleton<MessagePaginator>()
        .AddScoped<ICallbackQueryHandler, MessagePaginatorCallbackQueryHandler>();
}
