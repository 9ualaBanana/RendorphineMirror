namespace Telegram.Bot;

public record TelegramBotOptions
{
    internal const string Configuration = "TelegramBot";

    public string Token { get; init; } = null!;

    public Uri Host => _host ??= new(_Host.EndsWith('/') ? _Host : $"{_Host}/");
    Uri? _host;
    /// <remarks>
    /// Gets bound from <see cref="IConfiguration"/>.
    /// </remarks>
    string _Host { get; init; } = null!;
}

internal static class TelegramBotOptionsConfigurationExtension
{
    public static IServiceCollection ConfigureTelegramBotOptions(this IServiceCollection services)
        => services.AddOptions<TelegramBotOptions>()
            .BindConfiguration(TelegramBotOptions.Configuration, _ => _.BindNonPublicProperties = true)
        .Services;
}
