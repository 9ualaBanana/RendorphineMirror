using Telegram.Bot.MessagePagination.CallbackQuery;

namespace Telegram.Bot.MessagePagination;

internal static class MessagePaginationServiceCollectionExtension
{
    internal static IServiceCollection AddMessagePagination(this IServiceCollection services)
        => services
        .AddSingleton<ChunkedMessagesAutoStorage>()
        .AddSingleton<MessagePaginator>()
        .AddScoped<MessagePaginatorCallbackQueryHandler>();
}
