﻿using Telegram.Bot.MessagePagination;
using Telegram.Bot.Types;
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

    internal static ChatId ChatId(this Update update) =>
        update.Message?.Chat.Id ??
        update.CallbackQuery?.Message?.Chat.Id ??
        update.InlineQuery?.From.Id ??
        update.ChosenInlineResult?.From.Id ??
        update.ChannelPost?.Chat.Id ??
        update.EditedChannelPost?.Chat.Id ??
        update.ShippingQuery?.From.Id ??
        update.PreCheckoutQuery?.From.Id!;
}
