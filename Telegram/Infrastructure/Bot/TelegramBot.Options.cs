namespace Telegram.Infrastructure.Bot;

public partial class TelegramBot
{
    public record Options
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
}

static class TelegramBotOptionsExtensions
{
    internal static ITelegramBotBuilder ConfigureOptions(this ITelegramBotBuilder builder)
    {
        builder.Services.AddOptions<TelegramBot.Options>()
            .BindConfiguration(TelegramBot.Options.Configuration, _ => _.BindNonPublicProperties = true);
        return builder;
    }
}
