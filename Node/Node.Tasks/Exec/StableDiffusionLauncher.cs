namespace Node.Tasks.Exec;

public class StableDiffusionLaunchInfo
{
    public required string Prompt { get; init; }
    public string? NegativePrompt { get; init; }

    public int? Width { get; init; }
    public int? Height { get; init; }
    public int? Seed { get; init; }
}

public class StableDiffusionLauncher : PythonWrappedLaunch
{
    protected override PluginType PluginType => PluginType.StableDiffusion;

    public StableDiffusionLauncher(ILogger<StableDiffusionLauncher> logger) : base(logger) { }

    async Task Launch(StableDiffusionLaunchInfo info, string gentype, TaskFileList files, Action<ProcessLauncher>? modify = null)
    {
        var launcher = await CreateLauncherAsync();
        launcher.WithArgs(args =>
        {
            args.Add("gen", gentype);

            args.Add("-p", info.Prompt);
            args.Add("-o", files.New(FileFormat.Png).Path);

            args.AddArgumentIfNotNull("-n", info.NegativePrompt);
            args.AddArgumentIfNotNull("-w", info.Width);
            args.AddArgumentIfNotNull("-h", info.Height);
            args.AddArgumentIfNotNull("-s", info.Seed);
        });

        modify?.Invoke(launcher);
        await launcher.ExecuteAsync();
    }

    public async Task LaunchTxt2ImgAsync(StableDiffusionLaunchInfo info, TaskFileList files) =>
        await Launch(info, "txt2img", files);

    public async Task LaunchImg2ImgAsync(StableDiffusionLaunchInfo info, string inputimg, TaskFileList files) =>
        await Launch(info, "img2img", files, launcher => launcher.WithArgs(args => args.Add("--input", inputimg)));
}
