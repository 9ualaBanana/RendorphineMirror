using Telegram.Telegram.Updates;

namespace Telegram.Telegram.MessageChunker.Models;

public record MessageChunkerCallbackData : TelegramCallbackData<MessageChunkerCallbackQueryFlags>
{
    public MessageChunkerCallbackData(string callbackData)
        : base(new MessageChunkerCallbackData(ParseEnumValues(callbackData).Aggregate((r, n) => r |= n), ParseArguments(callbackData)))
    {
    }

    public MessageChunkerCallbackData(MessageChunkerCallbackQueryFlags Value, string[] Arguments) : base(Value, Arguments)
    {
    }
}
