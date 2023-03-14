using Microsoft.AspNetCore.Authentication;
using Telegram.MPlus;

namespace Telegram.Security.Authentication;

internal static class TelegramBotAuthenticationExtensions
{
    internal static AuthenticationBuilder AddMPlusViaTelegramChat(
        this AuthenticationBuilder authenticationBuilder,
        string? authenticationScheme = default)
    {
        authenticationBuilder.Services.AddMPlusClient();
        return authenticationBuilder.AddScheme<AuthenticationSchemeOptions, MPlusViaTelegramChatHandler>(
                authenticationScheme ?? MPlusViaTelegramChatDefaults.AuthenticationScheme, default
            );
    }
}
