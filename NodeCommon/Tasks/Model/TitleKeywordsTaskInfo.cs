namespace NodeCommon.Tasks.Model;

public class TitleKeywordsInputInfo : ITaskInputInfo
{
    public TaskInputType Type => TaskInputType.TitleKeywords;

    public readonly string Title;
    public readonly string[] Keywords;

    public TitleKeywordsInputInfo(string title, string[] keywords)
    {
        Title = title;
        Keywords = keywords;
    }
}
public class TitleKeywordsOutputInfo : ITaskOutputInfo
{
    public TaskOutputType Type => TaskOutputType.TitleKeywords;
    public Dictionary<string, TitleKeywordsData>? Data { get; init; }


    public record TitleKeywordsData(string? Title, string[]? Keywords);
}
