namespace Node.Tasks.Exec;

public class StableDiffusionLaunchInfo
{
    public required string Prompt { get; init; }
    public string? NegativePrompt { get; init; }

    public int? Width { get; init; }
    public int? Height { get; init; }
    public int? Seed { get; init; }
}
public static class StableDiffusionLauncher
{
    static async Task Launch(ITaskExecutionContext context, StableDiffusionLaunchInfo info, string gentype, TaskFileList files, Action<ProcessLauncher>? modify = null)
    {
        try
        {
            await launch();
        }
        catch
        {
            await install();
            await launch();
        }


        async Task install()
        {
            // TODO: move installation to installation not execution
            context.LogInfo("Installing stable diffusion...");

            await CondaInvoker.ExecutePowerShellAtWithCondaEnvAsync(
                context,
                PluginType.StableDiffusion,
                @".\sdcli install",
                null,
                context
            );

            context.LogInfo("Stable diffusion installed.");
        }
        async Task launch()
        {
            var launcher = new ProcessLauncher(context.GetPlugin(PluginType.StableDiffusion).Path) { ThrowOnStdErr = false, Logging = { Logger = context } }
                .WithArgs(args =>
                {
                    args.Add("gen", gentype);

                    args.Add("-p", info.Prompt);
                    args.Add("-o", files.New(FileFormat.Png).Path);

                    args.AddArgumentIfNotNull("-n", info.NegativePrompt);
                    args.AddArgumentIfNotNull("-w", info.Width);
                    args.AddArgumentIfNotNull("-h", info.Height);
                    args.AddArgumentIfNotNull("-s", info.Seed);
                })
                .AddOnOut(onread)
                .AddOnErr(onerr);

            modify?.Invoke(launcher);
            await launcher.ExecuteAsync(TimeSpan.FromMinutes(10));


            void onread(string line)
            {
                if (line.StartsWith("Unhandled exception: ", StringComparison.Ordinal)
                    || line.Contains("'gen' was not matched", StringComparison.Ordinal))
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

    public static async Task LaunchTxt2ImgAsync(ITaskExecutionContext context, StableDiffusionLaunchInfo info, TaskFileList files) =>
        await Launch(context, info, "txt2img", files);

    public static async Task LaunchImg2ImgAsync(ITaskExecutionContext context, StableDiffusionLaunchInfo info, string inputimg, TaskFileList files) =>
        await Launch(context, info, "img2img", files,
            launcher => launcher.WithArgs(args => args.Add("--input", inputimg))
        );
}
