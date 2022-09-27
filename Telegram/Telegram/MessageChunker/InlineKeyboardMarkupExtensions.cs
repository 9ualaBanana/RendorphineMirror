using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Telegram.MessageChunker.Models;

namespace Telegram.Telegram.MessageChunker;

public static class InlineKeyboardMarkupExtensions
{
    public static InlineKeyboardMarkup WithAddedButtonNext(this InlineKeyboardMarkup inlineKeyboardMarkup) => new(
        inlineKeyboardMarkup.InlineKeyboard.Append(new InlineKeyboardButton[]
        { InlineKeyboardButton.WithCallbackData(">", MessageChunkerCallbackQueryFlags.Next.ToString()) }));

    public static InlineKeyboardMarkup WithAddedButtonPrevious(this InlineKeyboardMarkup inlineKeyboardMarkup) => new(
        inlineKeyboardMarkup.InlineKeyboard.Append(new InlineKeyboardButton[]
        { InlineKeyboardButton.WithCallbackData("<", MessageChunkerCallbackQueryFlags.Previous.ToString()) }));
}
