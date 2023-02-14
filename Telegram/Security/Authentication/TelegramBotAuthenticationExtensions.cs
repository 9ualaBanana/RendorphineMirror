using Microsoft.AspNetCore.Authentication;
using Telegram.Models;

namespace Telegram.Security.Authentication;

internal static class TelegramBotAuthenticationExtensions
{
    internal static AuthenticationBuilder AddMPlusAuthenticationViaTelegramChat(
        this IServiceCollection services,
        string? authenticationScheme = default)
    {
        services.AddHttpClient<MPlusClient>();
        services.AddHttpClient<MPlusAuthenticationClient>();
        return services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, MPlusAuthenticationViaTelegramChatHandler>(
                authenticationScheme ?? MPlusAuthenticationViaTelegramChatDefaults.AuthenticationScheme, default
            );
    }
}
