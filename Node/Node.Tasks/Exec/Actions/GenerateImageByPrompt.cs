namespace Node.Tasks.Exec.Actions;

public class GenerateImageByPrompt : FilePluginActionInfo<GenerateImageByPromptInfo>
{
    public override TaskAction Name => TaskAction.GenerateImageByPrompt;
    public override ImmutableArray<PluginType> RequiredPlugins => ImmutableArray.Create(PluginType.StableDiffusion);
    protected override Type ExecutorType => typeof(Executor);

    public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats =>
        new[] { new[] { FileFormat.Png }, new[] { FileFormat.Jpeg }, Array.Empty<FileFormat>() };

    protected override OperationResult ValidateOutputFiles(TaskFilesCheckData files, GenerateImageByPromptInfo data) =>
        files.EnsureSingleOutputFile()
            .Next(output => TaskRequirement.EnsureFormat(output, FileFormat.Png));


    class Executor : ExecutorBase
    {
        public required ILifetimeScope Container { get; init; }

        public override async Task<TaskFileOutput> ExecuteUnchecked(TaskFileInput input, GenerateImageByPromptInfo data)
        {
            using var ctx = Container.ResolveForeign<StableDiffusionLauncher>(out var launcher);
            var inputfile = input.FirstOrDefault();

            var launchinfo = new StableDiffusionLaunchInfo()
            {
                Prompt = data.Prompt,
                NegativePrompt = data.NegativePrompt,
                Width = data.Width,
                Height = data.Height,
                Seed = data.Seed,
            };

            var output = new TaskFileOutput(input.ResultDirectory);
            var outfiles = output.Files.New();

            if (inputfile is not null)
                await launcher.LaunchImg2ImgAsync(launchinfo, inputfile.Path, outfiles);
            else await launcher.LaunchTxt2ImgAsync(launchinfo, outfiles);

            return output;
        }
    }
}
