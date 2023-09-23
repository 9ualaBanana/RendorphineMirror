namespace Node.Tasks.Exec.Actions;

public record TitleKeywordsInput(string Title, ImmutableArray<string> Keywords);
public record TitleKeywordsOutput(string Title, ImmutableArray<string> Keywords, string? Description = null);
public class GenerateTitleKeywords : FilePluginActionInfo<EitherFileTaskInput<TitleKeywordsInput>, TitleKeywordsOutput, GenerateTitleKeywordsInfo>
{
    public override TaskAction Name => TaskAction.GenerateTitleKeywords;
    public override ImmutableArray<PluginType> RequiredPlugins => ImmutableArray<PluginType>.Empty;
    protected override Type ExecutorType => typeof(Executor);

    public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats =>
        new[] { Array.Empty<FileFormat>(), new[] { FileFormat.Jpeg }, new[] { FileFormat.Png } };

    protected override void ValidateOutput(EitherFileTaskInput<TitleKeywordsInput> input, GenerateTitleKeywordsInfo data, TitleKeywordsOutput output) { }


    protected class Executor : ExecutorBase
    {
        public required IRegisteredTaskApi ApiTask { get; init; }
        public required Apis Api { get; init; }

        public override async Task<TitleKeywordsOutput> ExecuteUnchecked(EitherFileTaskInput<TitleKeywordsInput> input, GenerateTitleKeywordsInfo data)
        {
            return await input.If(
                async files =>
                {
                    var file = files.First();
                    var query = ApiBase.ToQuery(Api.AddSessionId(("taskid", ApiTask.Id)));

                    using var stream = File.OpenRead(file.Path);
                    using var content = new MultipartFormDataContent() { { new StreamContent(stream), "img", file.Format.ToMime() } };

                    return await Api.Api.ApiPost<TitleKeywordsOutput>($"https://t.microstock.plus:7899/generatetkd?{query}", "value", "generating tkd using gcloud vision + openai", content)
                        .ThrowIfError();
                },
                async tk =>
                {
                    var parameters = Api.AddSessionId(("taskid", ApiTask.Id), ("title", tk.Title), ("keywords", JsonConvert.SerializeObject(tk.Keywords)));
                    return await Api.Api.ApiPost<TitleKeywordsOutput>("https://t.microstock.plus:7899/openai/generatebettertk", "value", "generating better tk using openai", parameters)
                        .ThrowIfError();
                }
            );
        }
    }
}
