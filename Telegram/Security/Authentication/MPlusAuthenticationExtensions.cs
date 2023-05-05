using Microsoft.AspNetCore.Authentication;
using Telegram.MPlus;

namespace Telegram.Security.Authentication;

internal static class MPlusAuthenticationExtensions
{
    internal static AuthenticationBuilder AddMPlus(
        this AuthenticationBuilder authenticationBuilder,
        string? authenticationScheme = default)
    {
        authenticationBuilder.Services.AddMPlusClient();
        return authenticationBuilder.AddScheme<AuthenticationSchemeOptions, MPlusAuthenticationHandler>(
            authenticationScheme ?? MPlusAuthenticationDefaults.AuthenticationScheme, default);
    }
}
