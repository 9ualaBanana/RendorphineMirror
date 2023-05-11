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
        string webhookUrl = new Uri(_options.Host, $"telegram/{_options.Token}").ToString();
        await this.SetWebhookAsync(webhookUrl,
            allowedUpdates: new UpdateType[] { UpdateType.Message, UpdateType.CallbackQuery },
            dropPendingUpdates: true);
        Logger.LogTrace("Webhook is set", webhookUrl);
    }

    internal async Task NotifySubscribersAsync(string text, InlineKeyboardMarkup? replyMarkup = null)
    {
        foreach (var subscriber in Subscriptions)
            await SendMessageAsync_(subscriber, text, replyMarkup: replyMarkup);
    }

    internal async Task<Message> SendImageAsync_(
        ChatId chatId,
        InputOnlineFile image,
        string? caption = null,
        IReplyMarkup? replyMarkup = null,
        bool? disableNotification = null,
        bool? protectContent = null,
        CancellationToken cancellationToken = default) => await this.SendPhotoAsync(
            chatId,
            image,
            caption?.Sanitize(),
            ParseMode.MarkdownV2,
            captionEntities: null,
            disableNotification,
            protectContent,
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);

    internal async Task<Message> SendVideoAsync_(
        ChatId chatId,
        InputOnlineFile video,
        IReplyMarkup? replyMarkup = null,
        int? duration = null,
        int? width = null,
        int? height = null,
        InputMedia? thumb = default,
        string? caption = null,
        bool? supportsStreaming = default,
        bool? disableNotification = default,
        bool? protectContent = default,
        CancellationToken cancellationToken = default) => await this.SendVideoAsync(
            chatId,
            video,
            duration,
            width,
            height,
            thumb,
            caption?.Sanitize(),
            ParseMode.MarkdownV2,
            captionEntities: null,
            supportsStreaming,
            disableNotification,
            protectContent,
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);

    /// <remarks>
    /// If <paramref name="caption"/> is not <see langword="null"/>,
    /// it becomes the caption for the album and all captions of its individual media files are removed.
    /// </remarks>
    internal async Task<Message[]> SendAlbumAsync_(
        ChatId chatId,
        IEnumerable<IAlbumInputMedia> media,
        string? caption = null,
        bool? disableNotification = default,
        bool? protectContent = default,
        int? replyToMessageId = null,
        bool? allowSendingWithoutReply = default,
        CancellationToken cancellationToken = default)
    {
        if (caption is not null)
            media.Caption(caption);
            
        return await this.SendMediaGroupAsync(chatId, media, disableNotification, protectContent, replyToMessageId, allowSendingWithoutReply, cancellationToken);
    }

    internal async Task<Message> SendMessageAsync_(
        ChatId chatId,
        string text,
        InlineKeyboardMarkup? replyMarkup = null,
        bool? disableWebPagePreview = default,
        bool? disableNotification = default,
        bool? protectContent = default,
        int? replyToMessageId = null,
        bool? allowSendingWithoutReply = default,
        CancellationToken cancellationToken = default)
        => await (MessagePaginator.MustBeUsedToSend(text) ?
        _messagePaginator.SendPaginatedMessageAsyncUsing(this, chatId, text, replyMarkup, disableWebPagePreview, disableNotification, protectContent, replyToMessageId, allowSendingWithoutReply, cancellationToken) :
        SendMessageAsyncCore(chatId, text, replyMarkup, disableWebPagePreview, disableNotification, protectContent, replyToMessageId, allowSendingWithoutReply, cancellationToken));

    internal async Task<Message> SendMessageAsyncCore(
        ChatId chatId,
        string text,
        IReplyMarkup? replyMarkup = null,
        bool? disableWebPagePreview = default,
        bool? disableNotification = default,
        bool? protectContent = default,
        int? replyToMessageId = null,
        bool? allowSendingWithoutReply = default,
        CancellationToken cancellationToken = default) => await this.SendTextMessageAsync(
            chatId,
            text.Sanitize(),
            ParseMode.MarkdownV2,
            entities: null,
            disableWebPagePreview,
            disableNotification,
            protectContent,
            replyToMessageId,
            allowSendingWithoutReply,
            replyMarkup,
            cancellationToken);

    internal async Task<Message> EditMessageAsync_(
        ChatId chatId,
        int messageId,
        string text,
        InlineKeyboardMarkup? replyMarkup = null,
        bool? disableWebPagePreview = default,
        CancellationToken cancellationToken = default)
    {
        var editedMessage = await this.EditMessageTextAsync(
            chatId,
            messageId,
            text.Sanitize(),
            ParseMode.MarkdownV2,
            entities: null,
            disableWebPagePreview,
            replyMarkup,
            cancellationToken);
        if (replyMarkup is not null)
            editedMessage = await this.EditMessageReplyMarkupAsync(
                chatId,
                messageId,
                replyMarkup,
                cancellationToken);

        return editedMessage;
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
            .Replace("=", @"\=")
            .Replace("!", @"\!");
    }

    public static StringBuilder AppendHeader(this StringBuilder builder, string header)
        => builder
        .AppendLine(header)
        .AppendLine(HorizontalDelimeter);

    /// <summary>
    /// Sets <paramref name="caption"/> of the <paramref name="album"/>
    /// also removing captions from individual media files constituting it.
    /// </summary>
    /// <remarks>
    /// <paramref name="album"/> caption is set via the first media file constituting it.
    /// If any other constituent media files have their <see cref="InputMediaBase.Caption"/> set,
    /// then they are treated as captions of individual media files and not the <paramref name="album"/>.
    /// </remarks>
    internal static IEnumerable<IAlbumInputMedia> Caption(this IEnumerable<IAlbumInputMedia> album, string caption)
    {
        if (album.FirstOrDefault() is InputMediaBase album_)
        {
            album_.Caption = caption;
            foreach (var mediaFile in album.Skip(1))
                (mediaFile as InputMediaBase)!.Caption = null;
        }

        return album;
    }
}
