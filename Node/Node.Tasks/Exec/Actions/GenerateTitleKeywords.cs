namespace Node.Tasks.Exec.Actions;

public class GenerateTitleKeywordsInfo
{
    [JsonProperty("source")]
    [Default("ChatGPT")]
    public string Source { get; }

    public GenerateTitleKeywordsInfo(string source) => Source = source;
}

public class GenerateTitleKeywords : PluginAction<GenerateTitleKeywordsInfo>
{
    public override TaskAction Name => TaskAction.GenerateTitleKeywords;
    public override ImmutableArray<PluginType> RequiredPlugins => ImmutableArray<PluginType>.Empty;

    public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats => Array.Empty<FileFormat[]>();

    protected override OperationResult ValidateOutputFiles(TaskFilesCheckData files, GenerateTitleKeywordsInfo data) => true;

    public override async Task ExecuteUnchecked(ITaskExecutionContext context, TaskFiles files, GenerateTitleKeywordsInfo data)
    {
        var api = context.MPlusApi.ThrowIfNull();

        var parameters = Api.AddSessionId(api.SessionId,
            ("taskid", api.TaskId),
            ("title", files.InputFiles.OutputJson.ThrowIfNull()["Title"].ThrowIfNull().Value<string>().ThrowIfNull()),
            ("keywords", JsonConvert.SerializeObject(files.InputFiles.OutputJson.ThrowIfNull()["Keywords"].ThrowIfNull().ToObject<string[]>().ThrowIfNull()))
        );

        var result = await api.Api.ApiPost<GenerateTitleKeywordsResult>("https://t.microstock.plus:7899/openai/generatetitlekeywords", "value", "getting better title&kws using openai", parameters);
        files.OutputFiles.New().OutputJson = JObject.FromObject(result.ThrowIfError());
    }


    record GenerateTitleKeywordsResult(string Title, ImmutableArray<string> Keywords);
}
