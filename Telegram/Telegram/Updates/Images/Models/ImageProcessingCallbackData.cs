namespace Telegram.Telegram.Updates.Images.Models;

public record ImageProcessingCallbackData : MediaFileProcessingCallbackData<ImageProcessingQueryFlags>
{
    internal override IContentType ContentType => IContentType.Image;


    public ImageProcessingCallbackData(string callbackData)
        : base(new ImageProcessingCallbackData(ParseEnumValues(callbackData).Aggregate((r, n) => r |= n), ParseArguments(callbackData)))
    {
    }

    public ImageProcessingCallbackData(ImageProcessingQueryFlags Value, string[] Arguments) : base(Value, Arguments)
    {
    }
}
