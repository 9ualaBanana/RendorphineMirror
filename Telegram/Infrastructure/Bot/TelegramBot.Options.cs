namespace Telegram.Infrastructure.Bot;

public partial class TelegramBot
{
    public record Options
    {
        internal const string Configuration = "TelegramBot";

        public string Token { get; init; } = null!;

        public string Username { get; init; } = null!;

        internal Uri WebhookUri
        {
            get
            {
                var schemeAndAuthority = new Uri(Host.GetLeftPart(UriPartial.Authority));
                var pathBase = PathString.FromUriComponent(PathBase).Add(new PathString($"/{Token}")).ToUriComponent();

                return new Uri(schemeAndAuthority, pathBase);
            }
        }

        public Uri Host => _host ??= new(_Host.EndsWith('/') ? _Host : $"{_Host}/");
        Uri? _host;
        /// <inheritdoc cref="IConfigurationBoundPropertyDocumentation"/>
        string _Host { get; init; } = null!;

        internal string PathBase
        {
            get
            {
                if (_pathBase is not null) return _pathBase;

                _pathBase ??= _PathBase.StartsWith('/') ? _PathBase : $"/{_pathBase}";
                _pathBase ??= _pathBase!.EndsWith('/') ? _pathBase : $"{_PathBase}/";

                return _pathBase;
            }
        }
        string? _pathBase;
        /// <inheritdoc cref="IConfigurationBoundPropertyDocumentation"/>
        string _PathBase { get; init; } = null!;


        /// <remarks>
        /// Gets bound from <see cref="IConfiguration"/>.
        /// </remarks>
#pragma warning disable IDE0052 // Remove unread private members
        const object? IConfigurationBoundPropertyDocumentation = null;
#pragma warning restore IDE0052 // Remove unread private members
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
