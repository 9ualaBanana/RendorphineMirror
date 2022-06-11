using ReepoBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ReepoBot.Services.Telegram;

public class TelegramBot : TelegramBotClient
{
    public Subscriptions Subscriptions = new("subscriptions.txt");

    public TelegramBot(string token, HttpClient? httpClient = null, string? baseUrl = default)
        : base(token, httpClient, baseUrl)
    {
    }

    internal void TryNotifySubscribers(string text, ILogger logger, IReplyMarkup? replyMarkup = null)
    {
        foreach (var subscriber in Subscriptions)
        {
            _ = TrySendTextMessageAsync(subscriber, text, logger, replyMarkup);
        }
    }

    internal async Task<bool> TrySendTextMessageAsync(ChatId chatId, string text, ILogger logger, IReplyMarkup? replyMarkup = null)
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
    public static string Sanitize(this string unsanitizedString)
    {
        return unsanitizedString
            .Replace("|", @"\|")
            .Replace("[", @"\[")
            .Replace("]", @"\]")
            .Replace(".", @"\.")
            .Replace("-", @"\-")
            .Replace("_", @"\_")
            .Replace(">", @"\>")
            .Replace("(", @"\(")
            .Replace(")", @"\)");
    }
}
