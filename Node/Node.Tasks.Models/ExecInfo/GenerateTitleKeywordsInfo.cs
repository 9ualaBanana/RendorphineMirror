namespace Node.Tasks.Models.ExecInfo;

[JsonConverter(typeof(StringEnumConverter))]
public enum GenerateTitleKeywordsSource
{
    ChatGPT,
    VisionDenseCaptioning,
}
public class GenerateTitleKeywordsInfo
{
    [JsonProperty("source")]
    [Default(GenerateTitleKeywordsSource.ChatGPT)]
    public GenerateTitleKeywordsSource Source { get; }

    [JsonProperty("chatgpt")]
    public ChatGptInfo? ChatGpt { get; }

    public GenerateTitleKeywordsInfo(GenerateTitleKeywordsSource source) => Source = source;


    public record ChatGptInfo(string Model, string TitlePrompt, string KwPrompt);
}
