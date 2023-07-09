namespace Node.Tasks.Exec.Actions;

public class GenerateImageByMetaInfo
{
    [Default(ImageGenerationSource.StableDiffusion)]
    public ImageGenerationSource Source { get; }

    public GenerateImageByMetaInfo(ImageGenerationSource source) => Source = source;
}

public class GenerateImageByMeta : PluginAction<GenerateImageByMetaInfo>
{
    public override TaskAction Name => TaskAction.GenerateImageByMeta;
    public override ImmutableArray<PluginType> RequiredPlugins => ImmutableArray.Create(PluginType.StableDiffusion);

    public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats => Array.Empty<FileFormat[]>();

    protected override OperationResult ValidateOutputFiles(TaskFilesCheckData files, GenerateImageByMetaInfo data) =>
        files.EnsureSingleOutputFile()
            .Next(output => TaskRequirement.EnsureFormat(output, FileFormat.Png));

    public override async Task ExecuteUnchecked(ITaskExecutionContext context, TaskFiles files, GenerateImageByMetaInfo data)
    {
        // TODO:: TEMPORARY AND WILL BE REFACTORED
        var title = (files.InputFiles.OutputJson?["Title"]?.Value<string>()).ThrowIfNull();
        var keywords = (files.InputFiles.OutputJson?["Keywords"]?.ToObject<string[]>()).ThrowIfNull();

        var launchinfo = new StableDiffusionLaunchInfo()
        {
            Prompt = $"{title}, {string.Join(", ", keywords)}",
        };

        await StableDiffusionLauncher.LaunchTxt2ImgAsync(context, launchinfo, files.OutputFiles.New());
    }
}
