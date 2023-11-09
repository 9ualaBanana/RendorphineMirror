namespace Node.Tasks.Exec.Actions;

public record TitleKeywordsInput(string Title, ImmutableArray<string> Keywords);
public record TitleKeywordsOutput(string Title, ImmutableArray<string> Keywords);
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

                    using var img = Image.Load<Rgba32>(file.Path);

                    // google vision api accepts no more than 20MB images;
                    // downscale the image
                    if (img.Width > 2048)
                        img.Mutate(ctx => ctx.Resize(new Size((int) (img.Height / (img.Width / 2048f)), 2048)));

                    using var stream = new MemoryStream();
                    img.SaveAsJpeg(stream);
                    stream.Position = 0;

                    using var content = new MultipartFormDataContent()
                    {
                        { new StreamContent(stream), "img", file.Format.ToMime() },
                        { new StringContent(data.Source.ToString()), "source" },
                    };
                    if (data.ChatGpt is not null)
                    {
                        if (!string.IsNullOrEmpty(data.ChatGpt.Model))
                            content.Add(new StringContent(data.ChatGpt.Model), "model");
                        if (!string.IsNullOrEmpty(data.ChatGpt.TitlePrompt))
                            content.Add(new StringContent(data.ChatGpt.TitlePrompt), "titleprompt");
                        if (!string.IsNullOrEmpty(data.ChatGpt.KwPrompt))
                            content.Add(new StringContent(data.ChatGpt.KwPrompt), "kwprompt");
                    }

                    return await Api.Api.ApiPost<TitleKeywordsOutput>($"https://t.microstock.plus:7899/generatetkd?{query}", "value", "generating tkd using gcloud vision + openai", content)
                        .ThrowIfError();
                },
                async tk =>
                {
                    using var content = new MultipartFormDataContent()
                    {
                        { new StringContent(tk.Title), "title" },
                        { new StringContent(JsonConvert.SerializeObject(tk.Keywords)), "keywords" },
                    };
                    if (data.ChatGpt is not null)
                    {
                        if (!string.IsNullOrEmpty(data.ChatGpt.Model))
                            content.Add(new StringContent(data.ChatGpt.Model), "model");
                        if (!string.IsNullOrEmpty(data.ChatGpt.TitlePrompt))
                            content.Add(new StringContent(data.ChatGpt.TitlePrompt), "titleprompt");
                        if (!string.IsNullOrEmpty(data.ChatGpt.KwPrompt))
                            content.Add(new StringContent(data.ChatGpt.KwPrompt), "kwprompt");
                    }

                    var query = ApiBase.ToQuery(Api.AddSessionId(("taskid", ApiTask.Id)));
                    return await Api.Api.ApiPost<TitleKeywordsOutput>($"https://t.microstock.plus:7899/openai/generatebettertk?{query}", "value", "generating better tk using openai", content)
                        .ThrowIfError();
                }
            );
        }
    }
}
