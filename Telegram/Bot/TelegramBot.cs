using Microsoft.Extensions.Options;
using System.Text;
using Telegram.Bot.MessagePagination;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Models;

namespace Telegram.Bot;

public class TelegramBot : TelegramBotClient
{
    internal readonly ILogger Logger;

    readonly TelegramBotOptions _options;
    readonly MessagePaginator _messagePaginator;

    public Subscriptions Subscriptions = new("subscriptions.txt");

    public TelegramBot(IOptions<TelegramBotOptions> options, MessagePaginator messagePaginator, ILogger<TelegramBot> logger)
        : base(options.Value.Token)
    {
        _options = options.Value;
        _messagePaginator = messagePaginator;
        Logger = logger;
    }

    /// <summary>
    /// Must be called before <see cref="WebApplication.Run(string?)"/>.
    /// </summary>
    internal async Task InitializeAsync()
    {
        string webhookUrl = $"{_options.Host}/telegram/{_options.Token}";
        //string webhookUrl = $"{_options.Host}/telegram";
        await this.SetWebhookAsync(webhookUrl,
            allowedUpdates: new UpdateType[] { UpdateType.Message, UpdateType.CallbackQuery, UpdateType.ChatMember },
            dropPendingUpdates: true);
        Logger.LogDebug("Webhook for {Url} is set", webhookUrl);
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
            Logger.LogError(ex, "Following image couldn't be sent to {Chat}:\n\n{Image}", chatId, image);
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
            Logger.LogError(ex, "Following video couldn't be sent to {Chat}:\n{Video}\n{Thumbnail}",
                chatId, video, thumb);
        }
        return false;
    }

    internal async Task<Message?> TrySendMessageAsync(ChatId chatId, string text, IReplyMarkup? replyMarkup = null) => await
        (MessagePaginator.MustBeUsedToSend(text) && replyMarkup is null ?
        _messagePaginator.TrySendPaginatedMessageAsync(this, chatId, text) :
        TrySendMessageAsyncCore(chatId, text, replyMarkup));

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
        { Logger.LogError(ex, "Following message couldn't be sent to {Chat}:\n{Message}", chatId, text); return null; }
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
