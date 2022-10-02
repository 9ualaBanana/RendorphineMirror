using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Telegram.MessageChunker.Models;

namespace Telegram.Telegram.MessageChunker;

public static class TelegramMessageChunkerInlineKeyboardMarkup
{
    public static InlineKeyboardMarkup WithButtonsNextAndPrevious => new(new InlineKeyboardButton[]
    {
        InlineKeyboardButton.WithCallbackData("<", MessageChunkerCallbackQueryFlags.Previous.ToString()),
        InlineKeyboardButton.WithCallbackData(">", MessageChunkerCallbackQueryFlags.Next.ToString())
    });

    public static InlineKeyboardMarkup WithButtonNext => new(new InlineKeyboardButton[]
    { InlineKeyboardButton.WithCallbackData(">", MessageChunkerCallbackQueryFlags.Next.ToString()) });

    public static InlineKeyboardMarkup WithButtonPrevious => new(new InlineKeyboardButton[]
    { InlineKeyboardButton.WithCallbackData("<", MessageChunkerCallbackQueryFlags.Previous.ToString()) });
}
