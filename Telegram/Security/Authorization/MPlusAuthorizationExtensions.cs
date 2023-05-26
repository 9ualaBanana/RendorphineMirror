using Microsoft.AspNetCore.Authorization;
using Telegram.Infrastructure.Bot;

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
        => services
        .AddSingleton<IAuthorizationHandler, AccessLevelAuthorizationHandler>();
}
