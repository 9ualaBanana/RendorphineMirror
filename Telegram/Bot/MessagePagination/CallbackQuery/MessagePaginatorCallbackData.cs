using Telegram.Telegram.Updates;

namespace Telegram.Bot.MessagePagination.CallbackQuery;

public record MessagePaginatorCallbackData : TelegramCallbackData<MessagePaginatorCallbackQueryFlags>
{
    public MessagePaginatorCallbackData(string callbackData)
        : base(new MessagePaginatorCallbackData(ParseEnumValues(callbackData).Aggregate((r, n) => r |= n), ParseArguments(callbackData)))
    {
    }

    public MessagePaginatorCallbackData(MessagePaginatorCallbackQueryFlags Value, string[] Arguments) : base(Value, Arguments)
    {
    }
}
