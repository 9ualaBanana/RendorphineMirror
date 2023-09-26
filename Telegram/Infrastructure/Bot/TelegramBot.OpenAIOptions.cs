namespace Telegram.Infrastructure.Bot;

public partial class TelegramBot
{
    public record OpenAI
    {
        public record Options
        {
            internal const string Configuration = "OpenAI";

            public string ApiKey { get; init; } = default!;
        }
    }
}
