namespace Node.Tasks.Exec.Actions;

public class GenerateTKD : PluginAction<GenerateTitleKeywordsInfo>
{
    public override TaskAction Name => TaskAction.GenerateTitleKeywords;
    public override ImmutableArray<PluginType> RequiredPlugins => ImmutableArray<PluginType>.Empty;

    public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats =>
        new[] { Array.Empty<FileFormat>(), new[] { FileFormat.Jpeg }, new[] { FileFormat.Png } };

    protected override OperationResult ValidateOutputFiles(TaskFilesCheckData files, GenerateTitleKeywordsInfo data) => true;

    public override async Task ExecuteUnchecked(ITaskExecutionContext context, TaskFiles files, GenerateTitleKeywordsInfo data)
    {
        var api = context.MPlusApi.ThrowIfNull();

        var file = files.InputFiles.FirstOrDefault();

        if (file is not null)
        {
            var query = Api.ToGetContent(Api.AddSessionId(api.SessionId, ("taskid", api.TaskId)));

            using var stream = File.OpenRead(file.Path);
            using var content = new MultipartFormDataContent()
            {
                { new StreamContent(stream), "img", file.Format.ToMime() },
            };

            var result = await api.Api.ApiPost<GenerateTKDResult>($"https://t.microstock.plus:7899/generatetkd?{query}", "value", "generating tkd using gcloud vision + openai", content)
                .ThrowIfError();

            files.OutputFiles.New().OutputJson = JObject.FromObject(result);
        }
        else
        {
            var intitle = files.InputFiles.OutputJson.ThrowIfNull()["Title"].ThrowIfNull().Value<string>().ThrowIfNull();
            var inkws = JsonConvert.SerializeObject(files.InputFiles.OutputJson.ThrowIfNull()["Keywords"].ThrowIfNull().ToObject<string[]>().ThrowIfNull());
            var parameters = Api.AddSessionId(api.SessionId, ("taskid", api.TaskId), ("title", intitle), ("keywords", inkws));

            var result = await api.Api.ApiPost<GenerateTKResult>("https://t.microstock.plus:7899/openai/generatebettertk", "value", "generating better tk using openai", parameters)
                .ThrowIfError();

            files.OutputFiles.New().OutputJson = JObject.FromObject(result);
        }
    }


    record GenerateTKResult(string Title, ImmutableArray<string> Keywords);
    record GenerateTKDResult(string Title, string Description, ImmutableArray<string> Keywords) : GenerateTKResult(Title, Keywords);
}
