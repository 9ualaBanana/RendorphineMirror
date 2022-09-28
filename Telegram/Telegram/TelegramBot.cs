using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Models;
using Telegram.Telegram.MessageChunker.Services;

namespace Telegram.Telegram;

public class TelegramBot : TelegramBotClient
{
    ILogger _logger = null!;
    TelegramMessageChunker? _textChunker;


    public Subscriptions Subscriptions = new("subscriptions.txt");
    readonly public HttpClient HttpClient;

    public TelegramBot(string token, HttpClient? httpClient = null)
        : base(token, httpClient)
    {
        HttpClient = httpClient ?? new HttpClient();
    }

    internal static async Task<TelegramBot> Initialize(string token, string webhookHost)
    {
        var bot = new TelegramBot(token);
        await bot.SetWebhookAsync($"{webhookHost}/telegram");
        return bot;
    }

    internal TelegramBot UseLoggerFrom(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<TelegramBot>>();
        return this;
    }

    internal TelegramBot UseMessageChunkerFrom(IServiceProvider serviceProvider)
    {
        _textChunker = serviceProvider.GetRequiredService<TelegramMessageChunker>();
        return this;
    }

    internal async Task TryNotifySubscribersAboutImageAsync(
        InputOnlineFile image,
        string? caption = null,
        IReplyMarkup? replyMarkup = null)
    {
        foreach (var subscriber in Subscriptions)
            await TrySendImageAsync(subscriber, image, caption, replyMarkup);
    }

    internal async Task TryNotifySubscribersAboutVideoAsync(
        InputOnlineFile video,
        InputMedia? thumb = null,
        string? caption = null,
        int? width = null,
        int? height = null,
        IReplyMarkup? replyMarkup = null)
    {
        foreach (var subscriber in Subscriptions)
            await TrySendVideoAsync(subscriber, video, thumb, caption, width, height, replyMarkup);
    }

    internal async Task TryNotifySubscribersAsync(string text, IReplyMarkup? replyMarkup = null)
    {
        foreach (var subscriber in Subscriptions)
            await TrySendMessageAsync(subscriber, text, replyMarkup);
    }

    internal async Task<bool> TrySendImageAsync(
        ChatId chatId,
        InputOnlineFile image,
        string? caption = null,
        IReplyMarkup? replyMarkup = null)
    {
        try
        {
            await this.SendPhotoAsync(
                chatId,
                image,
                caption: caption?.Sanitize(),
                replyMarkup: replyMarkup,
                parseMode: ParseMode.MarkdownV2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Following image couldn't be sent to {Chat}:\n\n{Image}", chatId, image);
        }
        return false;
    }

    internal async Task<bool> TrySendVideoAsync(
        ChatId chatId,
        InputOnlineFile video,
        InputMedia? thumb = null,
        string? caption = null,
        int? width = null,
        int? height = null,
        IReplyMarkup? replyMarkup = null)
    {
        try
        {
            await this.SendVideoAsync(
                chatId,
                video,
                thumb: thumb,
                caption: caption?.Sanitize(),
                width: width,
                height: height,
                supportsStreaming: true,
                replyMarkup: replyMarkup,
                parseMode: ParseMode.MarkdownV2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Following video couldn't be sent to {Chat}:\n\n{Video}\n{Thumbnail}",
                chatId, video, thumb);
        }
        return false;
    }

    internal async Task<Message?> TrySendMessageAsync(ChatId chatId, string text, IReplyMarkup? replyMarkup = null)
    {
        if (_textChunker is null) return await TrySendMessageAsyncCore(chatId, text, replyMarkup);
        else return await _textChunker.TrySendChunkedMessageAsync(this, chatId, text);
    }

    internal async Task<Message?> TrySendMessageAsyncCore(ChatId chatId, string text, IReplyMarkup? replyMarkup = null)
    {
        try
        {
            return await this.SendTextMessageAsync(
                chatId,
                text.Sanitize(),
                replyMarkup: replyMarkup,
                parseMode: ParseMode.MarkdownV2);
        }
        catch (Exception ex)
        { _logger.LogError(ex, "Following message couldn't be sent to {Chat}:\n\n{Message}", chatId, text); return null; }
    }
}

public static class TelegramHelperExtensions
{
    public const string HorizontalDelimeter = "------------------------------------------------------------------------------------------------";

    public static string Sanitize(this string unsanitizedString)
    {
        return unsanitizedString
            .Replace("|", @"\|")
            .Replace("[", @"\[")
            .Replace("]", @"\]")
            .Replace("{", @"\{")
            .Replace("}", @"\}")
            .Replace(".", @"\.")
            .Replace("-", @"\-")
            .Replace("+", @"\+")
            .Replace("_", @"\_")
            .Replace(">", @"\>")
            .Replace("(", @"\(")
            .Replace(")", @"\)")
            .Replace("=", @"\=");
    }

    public static StringBuilder AppendHeader(this StringBuilder builder, string header) =>
        builder.AppendLine(header)
               .AppendLine(HorizontalDelimeter);
}
