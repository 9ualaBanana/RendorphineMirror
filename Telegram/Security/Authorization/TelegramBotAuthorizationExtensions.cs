using Microsoft.AspNetCore.Authorization;

namespace Telegram.Security.Authorization;

internal static class TelegramBotAuthorizationExtensions
{
    internal static IServiceCollection AddAuthorizationWithHandlers(this IServiceCollection services)
        => services
        .AddAuthorization()
        .AddSingleton<IAuthorizationHandler, MPlusAuthenticationAuthorizationHandler>()
        .AddSingleton<IAuthorizationHandler, AccessLevelAuthorizationHandler>();
}
