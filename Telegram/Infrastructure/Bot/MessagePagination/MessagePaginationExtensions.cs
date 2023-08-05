using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Infrastructure.CallbackQueries;

namespace Telegram.Infrastructure.Bot.MessagePagination;

static class MessagePaginationExtensions
{
    internal static ITelegramBotBuilder AddMessagePagination(this ITelegramBotBuilder builder)
    {
        builder.AddCallbackQueries();
        builder.Services.TryAddScoped_<ICallbackQueryHandler, MessagePaginatorCallbackQueryHandler>();
        builder.Services.TryAddSingleton<MessagePaginator>();
        builder.Services.TryAddSingleton<ChunkedMessagesAutoStorage>();
        builder.Services.TryAddSingleton<MessagePaginatorControlButtons>();

        return builder;
    }
}
