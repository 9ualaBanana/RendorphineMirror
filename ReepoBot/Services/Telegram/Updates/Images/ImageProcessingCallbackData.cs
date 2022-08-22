namespace ReepoBot.Services.Telegram.Updates.Images;

public record ImageProcessingCallbackData : TelegramCallbackData<ImageProcessingQueryFlags>
{
    public string FileRegistryKey => Arguments.First();

    public ImageProcessingCallbackData(string callbackData)
        : base(new ImageProcessingCallbackData(ParseEnumValues(callbackData).Aggregate((r, n) => r |= n), ParseArguments(callbackData)))
    {
    }

    public ImageProcessingCallbackData(ImageProcessingQueryFlags Value, string[] Arguments) : base(Value, Arguments)
    {
    }
}
