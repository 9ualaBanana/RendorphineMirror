namespace Node.Tasks.Exec.Actions;

public record TitleKeywords(string Title, ImmutableArray<string> Keywords);
public class GenerateTitleKeywords : GPluginAction<TitleKeywords, TitleKeywords, GenerateTitleKeywordsInfo>
{
    public override TaskAction Name => TaskAction.GenerateTitleKeywords;
    public override ImmutableArray<PluginType> RequiredPlugins => ImmutableArray<PluginType>.Empty;

    public required IMPlusApi MPlusApi { get; init; }

    protected override void ValidateInput(TitleKeywords input, GenerateTitleKeywordsInfo data) { }
    protected override void ValidateOutput(TitleKeywords input, GenerateTitleKeywordsInfo data, TitleKeywords output) { }

    public override async Task<TitleKeywords> ExecuteUnchecked(TitleKeywords input, GenerateTitleKeywordsInfo data)
    {
        var api = MPlusApi.ThrowIfNull();

        var parameters = Api.AddSessionId(api.SessionId,
            ("taskid", api.TaskId),
            ("title", input.Title),
            ("keywords", JsonConvert.SerializeObject(input.Keywords))
        );

        return await api.Api.ApiPost<TitleKeywords>("https://t.microstock.plus:7899/openai/generatetitlekeywords", "value", "getting better title&kws using openai", parameters)
            .ThrowIfError();
    }
}
