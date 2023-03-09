using Telegram.Callbacks;

namespace Telegram.CallbackQueries;

internal static class CallbackQueriesExtensions
{
    internal static IServiceCollection AddCallbackQueries(this IServiceCollection services)
        => services
        .AddSingleton(_
            => new CallbackQuerySerializer(new CallbackQuerySerializerOptions.Builder().BuildDefault())
        );
}
