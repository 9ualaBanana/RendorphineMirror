namespace Node.Tasks.Exec.Actions;

public class Topaz : PluginAction<TopazInfo>
{
    public override TaskAction Name => TaskAction.Topaz;
    public override ImmutableArray<PluginType> RequiredPlugins => ImmutableArray.Create(PluginType.TopazVideoAI);

    public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats =>
        new[] { new[] { FileFormat.Mov } };

    protected override OperationResult ValidateOutputFiles(TaskFilesCheckData files, TopazInfo data) =>
        files.EnsureSingleInputFile()
        .Next(input => files.EnsureSingleOutputFile()
        .Next(output => TaskRequirement.EnsureSameFormat(output, input)));

    public override async Task ExecuteUnchecked(ITaskExecutionContext context, TaskFiles files, TopazInfo data)
    {
        var ffmpeg = Path.Combine(context.GetPlugin(PluginType.TopazVideoAI).Path, "../ffmpeg.exe");

        var output = files.OutputFiles.New();
        foreach (var input in files.InputFiles)
        {
            var ffprobe = await FFProbe.Get(input.Path, context);

            var launcher = new FFmpegLauncher(ffmpeg)
            {
                Logger = context,
                ProgressSetter = new TaskExecutionContextProgressSetterAdapter(context),
                Input = { input.Path },
                Outputs =
                {
                    new FFmpegLauncherOutput()
                    {
                        Codec = FFmpegLauncher.CodecFromStream(ffprobe.VideoStream),
                        Output = output.New(input.Format).Path,
                    },
                },
            };

            AddFilters(data, ffprobe, launcher);
            await launcher.Execute();
        }
    }


    static void AddFilters(TopazInfo data, FFProbe.FFProbeInfo ffprobe, FFmpegLauncher launcher)
    {
        if (data.Operation == TopazOperation.Slowmo)
        {
            var filter = new ArgList()
            {
                string.Join(':', new ArgList()
                {
                    // slowmo filter with chronos model
                    "tvai_fi=model=chr-1",

                    // slowmo strength
                    $"slowmo={(data.X ?? 2).ToString(NumberFormats.Normal)}",
                }),

                // no replacing duplicate frames
                "rdt=-0.000001",

                "vram=1", "instances=1",
            };

            launcher.VideoFilters.Add(filter);
        }
        else if (data.Operation == TopazOperation.Upscale)
        {
            var w = ffprobe.VideoStream.Width * (data.X ?? 2);
            var h = ffprobe.VideoStream.Height * (data.X ?? 2);

            var filter = new ArgList()
            {
                // upscale with proteus filter
                "tvai_up=model=prob-3",

                "scale=0",

                // upscale size
                $"w={w.ToString(NumberFormats.Normal)}", $"h={h.ToString(NumberFormats.Normal)}",

                // enhancement filter (force-enabled by using upscale)
                "preblur=0", "noise=0", "details=0", "halo=0", "blur=0", "compression=0", "estimate=20",
                "vram=1", "instances=1",
            };

            launcher.VideoFilters.Add(new[]
            {
                string.Join(':', filter),

                // scaling
                $"scale=w={w.ToString(NumberFormats.Normal)}:h={h.ToString(NumberFormats.Normal)}",

                // scale filter
                "flags=lanczos:threads=0",
            });
        }
        else throw new TaskFailedException("Unknown operation");
    }
}
