using Autofac.Features.Indexed;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Node.Tasks.Exec.Actions;

public record TitleKeywordsInput(string Title, ImmutableArray<string> Keywords);
public record TitleKeywordsOutput(string Title, ImmutableArray<string> Keywords);
public class GenerateTitleKeywords : FilePluginActionInfo<EitherFileTaskInput<TitleKeywordsInput, MPlusItemInfo>, TitleKeywordsOutput, GenerateTitleKeywordsInfo>
{
    public override TaskAction Name => TaskAction.GenerateTitleKeywords;
    public override ImmutableArray<PluginType> RequiredPlugins => ImmutableArray<PluginType>.Empty;
    protected override Type ExecutorType => typeof(Executor);

    public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats =>
        new[] { Array.Empty<FileFormat>(), new[] { FileFormat.Jpeg }, new[] { FileFormat.Png } };

    protected override void ValidateOutput(EitherFileTaskInput<TitleKeywordsInput, MPlusItemInfo> input, GenerateTitleKeywordsInfo data, TitleKeywordsOutput output) { }


    protected class Executor : ExecutorBase
    {
        public required IRegisteredTaskApi ApiTask { get; init; }
        public required Apis Api { get; init; }

        public override async Task<TitleKeywordsOutput> ExecuteUnchecked(EitherFileTaskInput<TitleKeywordsInput, MPlusItemInfo> input, GenerateTitleKeywordsInfo data)
        {
            void addChatGptInfo(MultipartFormDataContent content)
            {
                if (data.ChatGpt is null) return;

                if (!string.IsNullOrEmpty(data.ChatGpt.Model))
                    content.Add(new StringContent(data.ChatGpt.Model), "model");
                if (!string.IsNullOrEmpty(data.ChatGpt.TitlePrompt))
                    content.Add(new StringContent(data.ChatGpt.TitlePrompt), "titleprompt");
                if (!string.IsNullOrEmpty(data.ChatGpt.KwPrompt))
                    content.Add(new StringContent(data.ChatGpt.KwPrompt), "kwprompt");
                if (!string.IsNullOrEmpty(data.ChatGpt.Prompt))
                    content.Add(new StringContent(data.ChatGpt.Prompt), "prompt");
            }

            var task = async () => await input.If(
                async files =>
                {
                    var file = files.First();
                    var query = ApiBase.ToQuery(Api.AddSessionId(("taskid", ApiTask.Id)));

                    using var img = Image.Load<Rgba32>(file.Path);

                    // google vision api accepts no more than 20MB images;
                    // and we don't need that much quality anyways
                    // downscale the image
                    if (img.Width > 500) img.Mutate(ctx => ctx.Resize(new Size((int) (img.Height / (img.Width / 500f)), 500)));
                    if (img.Height > 500) img.Mutate(ctx => ctx.Resize(new Size(500, (int) (img.Width / (img.Height / 500f)))));

                    using var stream = new MemoryStream();
                    img.SaveAsJpeg(stream);
                    stream.Position = 0;

                    using var content = new MultipartFormDataContent()
                    {
                        { new StreamContent(stream), "img", $"image{file.Format.AsExtension()}" },
                        { new StringContent(data.Source.ToString()), "source" },
                    };
                    addChatGptInfo(content);

                    return await new Api(new HttpClient() { Timeout = TimeSpan.FromHours(1) })
                        .ApiPost<TitleKeywordsOutput>($"https://t.microstock.plus:7899/generatetkd?{query}", "value", "generating tkd using gcloud vision + openai", content)
                        .ThrowIfError();
                },
                async tk =>
                {
                    using var content = new MultipartFormDataContent()
                    {
                        { new StringContent(tk.Title), "title" },
                        { new StringContent(JsonConvert.SerializeObject(tk.Keywords)), "keywords" },
                    };
                    addChatGptInfo(content);

                    var query = ApiBase.ToQuery(Api.AddSessionId(("taskid", ApiTask.Id)));
                    return await new Api(new HttpClient() { Timeout = TimeSpan.FromHours(1) })
                        .ApiPost<TitleKeywordsOutput>($"https://t.microstock.plus:7899/openai/generatebettertk?{query}", "value", "generating better tk using openai", content)
                        .ThrowIfError();
                },
                async mpitem =>
                {
                    using var content = new MultipartFormDataContent()
                    {
                        { new StringContent(mpitem.PreviewUrl), "url" },
                        { new StringContent(data.Source.ToString()), "source" },
                    };
                    addChatGptInfo(content);

                    var query = ApiBase.ToQuery(Api.AddSessionId(("taskid", ApiTask.Id)));
                    return await (Api.Api with { Client = new HttpClient() { Timeout = TimeSpan.FromHours(1) } })
                        .ApiPost<TitleKeywordsOutput>($"https://t.microstock.plus:7899/generatetkd?{query}", "value", "generating tkd using chatgpt", content)
                        .ThrowIfError();
                }
            );


            var lastex = null as Exception;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    return await task();
                }
                catch (Exception ex)
                {
                    lastex = ex;
                    Logger.LogError(ex, $"{ex.Message}; Retrying ({i}/5)");
                }

                await Task.Delay(5000);
            }

            throw lastex ?? new Exception("unknown error");
        }
    }
}
