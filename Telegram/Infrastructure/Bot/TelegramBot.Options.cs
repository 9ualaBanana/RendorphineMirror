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

        public string PathBase { get; init; } = default!;
    }
}

static class TelegramBotOptionsExtensions
{
    internal static ITelegramBotBuilder ConfigureOptions(this ITelegramBotBuilder builder)
    {
        builder.Services.AddOptions<TelegramBot.Options>()
            .BindConfiguration(TelegramBot.Options.Configuration)
            .Validate(_ => _.Host.OriginalString.EndsWith('/'),
                $"{nameof(TelegramBot.Options.Host)} must end with a path separator.")
            .Validate(_ => _.PathBase.StartsWith('/') &&  _.PathBase.EndsWith("/"),
                $"{nameof(TelegramBot.Options.PathBase)} must start and end with a path separator.")
            .ValidateOnStart();
        return builder;
    }
}
