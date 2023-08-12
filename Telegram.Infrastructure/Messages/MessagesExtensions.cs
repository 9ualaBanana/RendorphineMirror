using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;
using Telegram.Infrastructure.Middleware.UpdateRouting.UpdateTypeRouting;

namespace Telegram.Infrastructure.Messages;

static class MessagesExtensions
{
    internal static ITelegramBotBuilder AddMessagesCore(this ITelegramBotBuilder builder)
    {
        builder.Services
            .TryAddScoped_<IUpdateTypeRouter, MessageRouterMiddleware>()
            .TryAddScoped_<IMessageRouter, UnspecificMessageRouterMiddleware>();

        return builder;
    }
}
