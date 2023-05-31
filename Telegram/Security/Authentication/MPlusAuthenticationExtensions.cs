using Microsoft.AspNetCore.Authentication;
using Telegram.Infrastructure;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Messages;

namespace Telegram.Security.Authentication;

internal static class MPlusAuthenticationExtensions
{
    internal static ITelegramBotBuilder AddMPlusAuthentication(
        this ITelegramBotBuilder builder,
        string? authenticationScheme = default)
    {
        authenticationScheme ??= MPlusAuthenticationDefaults.AuthenticationScheme;

        builder.Services.TryAddScoped_<MessageHandler, AuthenticationMessageHandler>();
        builder.AddMPlusAuthenticationCore(authenticationScheme);

        return builder;
    }

    internal static ITelegramBotBuilder AddMPlusAuthenticationCore(
        this ITelegramBotBuilder builder,
        string? authenticationScheme = default)
    {
        authenticationScheme ??= MPlusAuthenticationDefaults.AuthenticationScheme;

        builder.AddAuthenticationManager();
        builder.Services.AddAuthentication(authenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, MPlusAuthenticationHandler>
            (authenticationScheme, default);

        return builder;
    }
}
