using System.Runtime.Serialization;

namespace ChatGptApi.OpenAiApi;

public class ChatRequest
{
    public required string Model { get; init; }
    public required IReadOnlyList<IMessage> Messages { get; init; }

    [JsonProperty("max_tokens")]
    public int? MaxTokens { get; init; }

    [JsonProperty("temperature")]
    public double? Temperature { get; init; }

    [JsonProperty("n")]
    public int? NumChoicesPerMessage { get; init; }



    public interface IMessage
    {
        ChatRole Role { get; }
        string AsString();
    }
    public record TextMessage(ChatRole Role, string Content) : IMessage
    {
        public string AsString() => Content;
    }
    public record ImageMessage(ChatRole Role, IReadOnlyList<IMessageContent> Content) : IMessage
    {
        public string AsString() => $"[{string.Join(", ", Content.Select(c => c.AsString()))}]";
    }


    public interface IMessageContent
    {
        string Type { get; }
        string AsString();
    }
    public class TextMessageContent : IMessageContent
    {
        public string Type => "text";
        public string Text { get; }

        public TextMessageContent(string text) => Text = text;

        public string AsString() => Text;
    }
    public class ImageMessageContent : IMessageContent
    {
        public string Type => "image_url";

        [JsonProperty("image_url")]
        public CSendMessageImageUrlContentUrl ImageUrl { get; }

        public ImageMessageContent(CSendMessageImageUrlContentUrl imageUrl) => ImageUrl = imageUrl;

        public string AsString() => $"<image; size {ImageUrl.Url.Length}, {ImageUrl.Detail} detail>";

        public static ImageMessageContent FromUrl(string url, ImageDetail detail) => new(new CSendMessageImageUrlContentUrl(url, detail));
        public static ImageMessageContent FromBase64(string mime, string base64, ImageDetail detail) => new(new CSendMessageImageUrlContentUrl($"data:{mime};base64," + base64, detail));


        public record CSendMessageImageUrlContentUrl(string Url, ImageDetail Detail);

        public enum ImageDetail
        {
            [EnumMember(Value = "high")] High,
            [EnumMember(Value = "low")] Low,
        }
    }
}
