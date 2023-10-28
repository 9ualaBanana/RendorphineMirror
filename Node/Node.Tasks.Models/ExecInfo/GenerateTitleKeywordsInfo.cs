namespace Node.Tasks.Models.ExecInfo;

public class GenerateTitleKeywordsInfo
{
    [JsonProperty("source")]
    [Default("ChatGPT")]
    public string Source { get; }

    [JsonProperty("chatgpt")]
    public ChatGptInfo? ChatGpt { get; }

    public GenerateTitleKeywordsInfo(string source) => Source = source;


    public record ChatGptInfo(string Model, string TitlePrompt, string DescrPrompt, string KwPrompt);
}
