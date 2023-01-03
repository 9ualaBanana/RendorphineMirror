namespace Telegram.Telegram.Updates.Images.Models;

public record VideoProcessingCallbackData : MediaFileProcessingCallbackData<VideoProcessingQueryFlags>
{
    internal override IContentType ContentType => IContentType.Video;


    public VideoProcessingCallbackData(string callbackData)
        : base(new VideoProcessingCallbackData(ParseEnumValues(callbackData).Aggregate((r, n) => r |= n), ParseArguments(callbackData)))
    {
    }

    public VideoProcessingCallbackData(VideoProcessingQueryFlags Value, string[] Arguments) : base(Value, Arguments)
    {
    }
}
