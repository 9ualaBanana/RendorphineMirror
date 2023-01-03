namespace Telegram.Telegram.Updates.Images.Models;

public record VectorizerCallbackData : MediaFileProcessingCallbackData<VectorizerQueryFlags>
{
    internal int Polygonality => int.Parse(Arguments.Last());
    internal override IContentType ContentType => IContentType.Image;


    public VectorizerCallbackData(string callbackData)
        : base(new VectorizerCallbackData(ParseEnumValues(callbackData).Aggregate((r, n) => r |= n), ParseArguments(callbackData)))
    {
    }

    public VectorizerCallbackData(VectorizerQueryFlags value, string[] arguments) : base(value, arguments)
    {
    }
}
