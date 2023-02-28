using Microsoft.AspNetCore.Authentication;
using Telegram.Models;

namespace Telegram.Security.Authentication;

internal static class TelegramBotAuthenticationExtensions
{
    internal static AuthenticationBuilder AddMPlusViaTelegramChat(
        this AuthenticationBuilder authenticationBuilder,
        string? authenticationScheme = default)
    {
        authenticationBuilder.Services.AddHttpClient<MPlusClient>();
        return authenticationBuilder.AddScheme<AuthenticationSchemeOptions, MPlusViaTelegramChatHandler>(
                authenticationScheme ?? MPlusViaTelegramChatDefaults.AuthenticationScheme, default
            );
    }
}
