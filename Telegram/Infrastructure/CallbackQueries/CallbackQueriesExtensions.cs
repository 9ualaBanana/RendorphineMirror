using Telegram.Callbacks;
using Telegram.Infrastructure.CallbackQueries.Serialization;
using Telegram.Infrastructure.Middleware.UpdateRouting.UpdateTypeRouting;

namespace Telegram.Infrastructure.CallbackQueries;

static class CallbackQueriesExtensions
{
    internal static IServiceCollection AddCallbackQueries(
        this IServiceCollection services,
        CallbackQuerySerializerOptions? options = default)
        => services
        .AddScoped<IUpdateTypeRouter, CallbackQueryRouterMiddleware>()
        .AddSingleton(services => new CallbackQuerySerializer(
            options ??
            services.GetService<CallbackQuerySerializerOptions>() ??
            new CallbackQuerySerializerOptions.Builder().BuildDefault()
            )
        );
}
