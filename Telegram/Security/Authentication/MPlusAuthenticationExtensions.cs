﻿using Microsoft.AspNetCore.Authentication;
using Telegram.Infrastructure;
using Telegram.Infrastructure.Bot;

namespace Telegram.Security.Authentication;

internal static class MPlusAuthenticationExtensions
{
    internal static ITelegramBotBuilder AddMPlusAuthentication(
        this ITelegramBotBuilder builder,
        string? authenticationScheme = default)
    {
        authenticationScheme ??= MPlusAuthenticationDefaults.AuthenticationScheme;

        builder
            .AddAuthenticationManager()

            .Services
            .AddScoped<MessageHandler, AuthenticationMessageHandler>()
            .AddAuthentication(authenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, MPlusAuthenticationHandler>
                (authenticationScheme, default);

        return builder;
    }
}
