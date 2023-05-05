using Telegram.Bot.MessagePagination;
using Telegram.Persistence;

namespace Telegram.Bot;

internal static class TelegramBotExtensions
{
    internal static IWebHostBuilder AddTelegramBot(this IWebHostBuilder builder)
        => builder
        .ConfigureAppConfiguration(_ => _
            .AddJsonFile("botsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"botsettings.{Environments.Development}.json", optional: true, reloadOnChange: false))
        .ConfigureServices(_ => _
            .AddSingleton<TelegramBot>().AddMessagePagination()
                .ConfigureTelegramBotOptions()
            .AddDbContext<TelegramBotDbContext>());
}
