using Telegram.Bot.MessagePagination;
using Telegram.Persistence;

namespace Telegram.Bot;

internal static class TelegramBotExtensions
{
    internal static IServiceCollection AddTelegramBotUsing(this IServiceCollection services, ConfigurationManager configuration)
    {
        configuration
            .AddJsonFile("botsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"botsettings.{Environments.Development}.json", optional: true, reloadOnChange: false);

        return services
            .ConfigureTelegramBotOptionsUsing(configuration)
            .AddSingleton<TelegramBot>()
                .AddMessagePagination()
            .AddDbContext<TelegramBotDbContext>();
    }
}
