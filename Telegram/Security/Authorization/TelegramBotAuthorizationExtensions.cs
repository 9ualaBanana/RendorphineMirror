using Microsoft.AspNetCore.Authorization;

namespace Telegram.Security.Authorization;

internal static class TelegramBotAuthorizationExtensions
{
    internal static IServiceCollection AddAuthorizationHandlers(this IServiceCollection services)
        => services
        .AddSingleton<IAuthorizationHandler, MPlusAuthenticationAuthorizationHandler>()
        .AddSingleton<IAuthorizationHandler, AccessLevelAuthorizationHandler>();
}
