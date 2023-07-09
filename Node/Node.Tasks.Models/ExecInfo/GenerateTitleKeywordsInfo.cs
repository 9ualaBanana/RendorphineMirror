namespace Node.Tasks.Models.ExecInfo;

public class GenerateTitleKeywordsInfo
{
    [JsonProperty("source")]
    [Default("ChatGPT")]
    public string Source { get; }

    public GenerateTitleKeywordsInfo(string source) => Source = source;
}
