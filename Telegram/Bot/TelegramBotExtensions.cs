using Telegram.Bot.MessagePagination;
using Telegram.Bot.Types;
using Telegram.Persistence;

namespace Telegram.Bot;

internal static class TelegramBotExtensions
{
    internal static IWebHostBuilder AddTelegramBot(this IWebHostBuilder builder)
        => builder
        .ConfigureAppConfiguration(_ => _
            .AddJsonFile("botsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"botsettings.{Environments.Development}.json", optional: true, reloadOnChange: true))
        .ConfigureServices(_ => _
            .AddSingleton<TelegramBot>().AddMessagePagination()
                .ConfigureTelegramBotOptions()
                .ConfigureTelegramBotExceptionHandlerOptions()
            .AddDbContext<TelegramBotDbContext>());

    internal static ChatId ChatId(this Update update) =>
        update.Message?.Chat.Id ??
        update.CallbackQuery?.Message?.Chat.Id ??
        update.InlineQuery?.From.Id ??
        update.ChosenInlineResult?.From.Id ??
        update.ChannelPost?.Chat.Id ??
        update.EditedChannelPost?.Chat.Id ??
        update.ShippingQuery?.From.Id ??
        update.PreCheckoutQuery?.From.Id!;

    internal static User From(this Update update) =>
        update.Message?.From ??
        update.CallbackQuery?.Message?.From ??
        update.InlineQuery?.From ??
        update.ChosenInlineResult?.From ??
        update.ChannelPost?.From ??
        update.EditedChannelPost?.From ??
        update.ShippingQuery?.From ??
        update.PreCheckoutQuery?.From!;
}
