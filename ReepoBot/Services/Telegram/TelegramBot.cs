using ReepoBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace ReepoBot.Services.Telegram;

public class TelegramBot : TelegramBotClient
{
    public Subscriptions Subscriptions = new("subscriptions.txt");

    public TelegramBot(string token, HttpClient? httpClient = null, string? baseUrl = default)
        : base(token, httpClient, baseUrl)
    {
    }

    internal void TryNotifySubscribers(
        InputOnlineFile video,
        ILogger logger,
        InputMedia? thumb = null,
        string? caption = null,
        int? width = null,
        int? height = null,
        IReplyMarkup? replyMarkup = null)
    {
        foreach (var subscriber in Subscriptions)
        {
            _ = TrySendVideoAsync(subscriber, video, logger, thumb, caption, width, height, replyMarkup);
        }
    }

    internal void TryNotifySubscribers(string text, ILogger logger, IReplyMarkup? replyMarkup = null)
    {
        foreach (var subscriber in Subscriptions)
        {
            _ = TrySendMessageAsync(subscriber, text, logger, replyMarkup);
        }
    }

    internal async Task<bool> TrySendVideoAsync(ChatId chatId,
        InputOnlineFile video,
        ILogger logger,
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
            logger.LogError(ex, "Following video couldn't be sent to {Chat}:\n\n{Video}\n{Thumbnail}",
                chatId, video, thumb);
        }
        return false;
    }

    internal async Task<bool> TrySendMessageAsync(ChatId chatId, string text, ILogger logger, IReplyMarkup? replyMarkup = null)
    {
        try
        {
            await this.SendTextMessageAsync(
                chatId,
                text.Sanitize(),
                replyMarkup: replyMarkup,
                parseMode: ParseMode.MarkdownV2);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Following message couldn't be sent to {Chat}:\n\n{Message}", chatId, text);
        }
        return false;
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
            .Replace(".", @"\.")
            .Replace("-", @"\-")
            .Replace("*", @"\*")
            .Replace("_", @"\_")
            .Replace(">", @"\>")
            .Replace("(", @"\(")
            .Replace(")", @"\)")
            .Replace("=", @"\=");
    }
}
