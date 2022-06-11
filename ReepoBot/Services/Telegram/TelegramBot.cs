using ReepoBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramHelper;

namespace ReepoBot.Services.Telegram;

public class TelegramBot : TelegramBotClient
{
    public Subscriptions Subscriptions = new("subscriptions.txt");

    public TelegramBot(string token)
        : base(token)
    {
    }

    public async Task TrySendTextMessageAsync(ChatId chatId, string text, ILogger logger, IReplyMarkup? replyMarkup = null)
    {
        try
        {
            await this.SendTextMessageAsync(
                chatId,
                text.Sanitize(),
                replyMarkup: replyMarkup,
                parseMode: ParseMode.MarkdownV2);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Following message couldn't be sent:\n\n{Message}", text);
        }
    }
}
