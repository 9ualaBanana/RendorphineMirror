﻿namespace ReepoBot.Services.Telegram.Updates.Images;

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
