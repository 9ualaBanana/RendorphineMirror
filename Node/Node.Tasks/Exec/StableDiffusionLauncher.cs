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
    static async Task Launch(StableDiffusionLaunchInfo info, string gentype, TaskFileList files, PluginList plugins, IProgressSetter progressSetter, ILogger logger, Action<ProcessLauncher>? modify = null)
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
            logger.LogInformation("Installing stable diffusion...");

            await CondaInvoker.ExecutePowerShellAtWithCondaEnvAsync(
                plugins,
                PluginType.StableDiffusion,
                @".\sdcli install",
                null,
                logger
            );

            logger.LogInformation("Stable diffusion installed.");
        }
        async Task launch()
        {
            var launcher = new ProcessLauncher(plugins.GetPlugin(PluginType.StableDiffusion).Path)
            {
                ThrowOnStdErr = false,
                Logging = { ILogger = logger },
                Timeout = TimeSpan.FromMinutes(10),
            }
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
            await launcher.ExecuteAsync();


            void onread(string line)
            {
                if (line.StartsWith("Unhandled exception: ", StringComparison.Ordinal)
                    || line.Contains("'gen' was not matched", StringComparison.Ordinal))
                    throw new Exception(line);

                if (!line.StartsWith("Progress: ", StringComparison.Ordinal)) return;

                var progress = int.Parse(line.AsSpan()["Progress: ".Length..^"%".Length], CultureInfo.InvariantCulture) / 100d;
                progressSetter.Set(progress);
            }
            void onerr(string line)
            {
                if (line == "Process not running, starting") return;

                throw new Exception(line);
            }
        }
    }

    public static async Task LaunchTxt2ImgAsync(StableDiffusionLaunchInfo info, TaskFileList files, PluginList plugins, IProgressSetter progress, ILogger logger) =>
        await Launch(info, "txt2img", files, plugins, progress, logger);

    public static async Task LaunchImg2ImgAsync(StableDiffusionLaunchInfo info, string inputimg, TaskFileList files, PluginList plugins, IProgressSetter progress, ILogger logger) =>
        await Launch(info, "img2img", files, plugins, progress, logger,
            launcher => launcher.WithArgs(args => args.Add("--input", inputimg))
        );
}
