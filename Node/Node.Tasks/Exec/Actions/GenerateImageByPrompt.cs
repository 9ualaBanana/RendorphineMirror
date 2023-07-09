namespace Node.Tasks.Exec.Actions;

public class GenerateImageByPromptInfo
{
    [Default(ImageGenerationSource.StableDiffusion)]
    public ImageGenerationSource Source { get; }

    public string Prompt { get; init; }

    [JsonProperty("negprompt")]
    public string? NegativePrompt { get; init; }

    public int? Width { get; init; }
    public int? Height { get; init; }
    public int? Seed { get; init; }

    public GenerateImageByPromptInfo(ImageGenerationSource source, string prompt)
    {
        Source = source;
        Prompt = prompt;
    }
}
public class GenerateImageByPrompt : PluginAction<GenerateImageByPromptInfo>
{
    public override TaskAction Name => TaskAction.GenerateImageByPrompt;
    public override ImmutableArray<PluginType> RequiredPlugins => ImmutableArray.Create(PluginType.StableDiffusion);

    public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats =>
        new[] { new[] { FileFormat.Png }, new[] { FileFormat.Jpeg }, Array.Empty<FileFormat>() };

    protected override OperationResult ValidateOutputFiles(TaskFilesCheckData files, GenerateImageByPromptInfo data) =>
        files.EnsureSingleOutputFile()
            .Next(output => TaskRequirement.EnsureFormat(output, FileFormat.Png));

    public override async Task ExecuteUnchecked(ITaskExecutionContext context, TaskFiles files, GenerateImageByPromptInfo data)
    {
        var inputfile = files.InputFiles.FirstOrDefault();

        var launchinfo = new StableDiffusionLaunchInfo()
        {
            Prompt = data.Prompt,
            NegativePrompt = data.NegativePrompt,
            Width = data.Width,
            Height = data.Height,
            Seed = data.Seed,
        };

        var output = files.OutputFiles.New();

        if (inputfile is not null)
            await StableDiffusionLauncher.LaunchImg2ImgAsync(context, launchinfo, inputfile.Path, output);
        else await StableDiffusionLauncher.LaunchTxt2ImgAsync(context, launchinfo, output);
    }
}
