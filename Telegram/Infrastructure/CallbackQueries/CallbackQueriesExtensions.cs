using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Callbacks;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.CallbackQueries.Serialization;
using Telegram.Infrastructure.Middleware.UpdateRouting.UpdateTypeRouting;

namespace Telegram.Infrastructure.CallbackQueries;

static class CallbackQueriesExtensions
{
    internal static ITelegramBotBuilder AddCallbackQueries(
        this ITelegramBotBuilder builder,
        CallbackQuerySerializerOptions? options = default)
    {
        builder.Services.TryAddScoped_<IUpdateTypeRouter, CallbackQueryRouterMiddleware>();
        builder.Services.TryAddSingleton(services => new CallbackQuerySerializer(
            options ??
            services.GetService<CallbackQuerySerializerOptions>() ??
            new CallbackQuerySerializerOptions.Builder().BuildDefault())
        );

        return builder;
    }
}
