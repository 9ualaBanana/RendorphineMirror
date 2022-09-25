using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Telegram.Services.MessageChunker;

public static class InlineKeyboardExtensions
{
    public static InlineKeyboardMarkup WithAddedButtonNext(this InlineKeyboardMarkup inlineKeyboardMarkup) => new(
        inlineKeyboardMarkup.InlineKeyboard.Append(new InlineKeyboardButton[]
        { InlineKeyboardButton.WithCallbackData(">", ChunkedMessageCallbackQueryFlags.Next.ToString()) }));

    public static InlineKeyboardMarkup WithAddedButtonPrevious(this InlineKeyboardMarkup inlineKeyboardMarkup) => new(
        inlineKeyboardMarkup.InlineKeyboard.Append(new InlineKeyboardButton[]
        { InlineKeyboardButton.WithCallbackData("<", ChunkedMessageCallbackQueryFlags.Previous.ToString()) }));
}
