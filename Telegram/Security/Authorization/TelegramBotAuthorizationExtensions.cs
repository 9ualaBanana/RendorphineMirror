using Microsoft.AspNetCore.Authorization;

namespace Telegram.Security.Authorization;

internal static class TelegramBotAuthorizationExtensions
{
    internal static IServiceCollection AddMPlusAuthorization(this IServiceCollection services)
        => services
        .AddAuthorization()
        .AddSingleton<IAuthorizationHandler, AccessLevelAuthorizationHandler>();
}
