using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Telegram.Security.Authorization;

internal static class MPlusAuthorizationExtensions
{
    internal static ITelegramBotBuilder AddMPlusAuthorization(this ITelegramBotBuilder builder)
    {
        builder.Services
            .AddAuthorization()
            .AddMPlusAuthorizationHandlers();

        return builder;
    }

    static IServiceCollection AddMPlusAuthorizationHandlers(this IServiceCollection services)
    {
        services.TryAddSingleton<IAuthorizationHandler, AccessLevelAuthorizationHandler>();

        return services;
    }
}
