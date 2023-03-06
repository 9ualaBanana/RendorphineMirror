﻿using Microsoft.Extensions.Options;

namespace Telegram.Bot;

public record TelegramBotOptions(string Token, Uri Host)
{
    internal const string Section = "TelegramBot";

    /// <summary>
    /// Shouldn't be called. Required for use with <see cref="IOptions{TOptions}"/>.
    /// </summary>
    public TelegramBotOptions() : this(null!, null!)
    {
    }
}

internal static class TelegramBotOptionsConfigurationExtension
{
    public static IServiceCollection ConfigureTelegramBotOptionsUsing(this IServiceCollection services, IConfiguration configuration)
    {
        var configurationSection = configuration.GetRequiredSection(TelegramBotOptions.Section);
        return services.Configure<TelegramBotOptions>(configurationSection);
    }
}