namespace Telegram.Infrastructure.Bot;

public partial class TelegramBot
{
    public record Options
    {
        internal const string Configuration = "TelegramBot";

        public string Token { get; init; } = default!;

        public string Username { get; init; } = default!;

        internal Uri WebhookUri
        {
            get
            {
                var schemeAndAuthority = new Uri(Host.GetLeftPart(UriPartial.Authority));
                var pathBase = PathString.FromUriComponent(PathBase).Add(new PathString($"/{Token}")).ToUriComponent();

                return new Uri(schemeAndAuthority, pathBase);
            }
        }

        public Uri Host { get; init; } = default!;

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
        string _PathBase { get; init; } = null!;
    }
}

static class TelegramBotOptionsExtensions
{
    internal static ITelegramBotBuilder ConfigureOptions(this ITelegramBotBuilder builder)
    {
        builder.Services.AddOptions<TelegramBot.Options>()
            .BindConfiguration(TelegramBot.Options.Configuration, _ => _.BindNonPublicProperties = true)
            .Validate(_ => Path.EndsInDirectorySeparator(_.Host.OriginalString),
                $"{nameof(TelegramBot.Options.Host)} must end with a path separator.")
            .ValidateOnStart();
        return builder;
    }
}
