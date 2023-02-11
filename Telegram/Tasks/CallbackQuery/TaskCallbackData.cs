using Telegram.Telegram.Updates;

namespace Telegram.Tasks.CallbackQuery;

public record TaskCallbackData : TelegramCallbackData<TaskCallbackQueryFlags>
{
    public string TaskId => Arguments.First();

    public TaskCallbackData(string callbackData)
        : base(new TaskCallbackData(ParseEnumValues(callbackData).Aggregate((r, n) => r |= n), ParseArguments(callbackData)))
    {
    }

    public TaskCallbackData(TaskCallbackQueryFlags Value, string[] Arguments) : base(Value, Arguments)
    {
    }
}
