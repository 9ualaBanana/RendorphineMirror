using Telegram.Telegram.Updates;

namespace Telegram.Telegram.Updates.Tasks.Models;

public record TaskCallbackData : TelegramCallbackData<TaskQueryFlags>
{
    public string TaskId => Arguments.First();

    public TaskCallbackData(string callbackData)
        : base(new TaskCallbackData(ParseEnumValues(callbackData).Aggregate((r, n) => r |= n), ParseArguments(callbackData)))
    {
    }

    public TaskCallbackData(TaskQueryFlags Value, string[] Arguments) : base(Value, Arguments)
    {
    }
}
