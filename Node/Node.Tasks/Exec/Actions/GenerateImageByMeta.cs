namespace Node.Tasks.Exec.Actions;

public class GenerateImageByMetaInfo
{
    [JsonProperty("source")]
    [Default("StableDiffusion")]
    public string Source { get; }

    public GenerateImageByMetaInfo(string source) => Source = source;
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
        var sdcli = context.GetPlugin(PluginType.StableDiffusion);
        var info = files.InputFiles.OutputJson.ThrowIfNull().ToObject<GenerateImageByMetaInputInfo>().ThrowIfNull();

        await new ProcessLauncher(sdcli.Path) { ThrowOnStdErr = false, Logging = { Logger = context } }
            .WithArgs(args =>
            {
                args.Add("gen", "txt2img");

                args.Add("-p", info.Prompt);
                args.Add("-o", files.OutputFiles.New().New(FileFormat.Png).Path);

                args.AddArgumentIfNotNull("-n", info.NegativePrompt);
                args.AddArgumentIfNotNull("-w", info.Width);
                args.AddArgumentIfNotNull("-h", info.Height);
                args.AddArgumentIfNotNull("-s", info.Seed);
            })
            .AddOnOut(onread)
            .AddOnErr(onerr)
            .ExecuteAsync();


        void onread(string line)
        {
            if (line.StartsWith("Unhandled exception: ", StringComparison.Ordinal))
                throw new Exception(line);

            if (!line.StartsWith("Progress: ", StringComparison.Ordinal)) return;

            var progress = int.Parse(line.AsSpan()["Progress: ".Length..^"%".Length], CultureInfo.InvariantCulture) / 100d;
            context.SetProgress(progress);
        }
        void onerr(string line)
        {
            if (line == "Process not running, starting") return;

            throw new Exception(line);
        }
    }
}
