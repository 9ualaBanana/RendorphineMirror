namespace Node.Tasks.Exec.Actions;

public class Topaz : FilePluginActionInfo<TopazInfo>
{
    public override TaskAction Name => TaskAction.Topaz;
    public override ImmutableArray<PluginType> RequiredPlugins => ImmutableArray.Create(PluginType.TopazVideoAI);
    protected override Type ExecutorType => typeof(Executor);

    public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats =>
        new[] { new[] { FileFormat.Jpeg }, new[] { FileFormat.Png }, new[] { FileFormat.Mov } };

    protected override OperationResult ValidateOutputFiles(TaskFilesCheckData files, TopazInfo data) =>
        files.EnsureSingleInputFile()
        .Next(input => files.EnsureSingleOutputFile()
        .Next(output => TaskRequirement.EnsureSameFormat(output, input)));


    class Executor : ExecutorBase
    {
        public required DataDirs Dirs { get; init; }

        public override async Task<TaskFileOutput> ExecuteUnchecked(TaskFileInput input, TopazInfo data)
        {
            var output = new TaskFileOutput(input.ResultDirectory);
            var outfiles = output.Files.New();
            var inputfile = input.Single();

            if (data.Operation is TopazOperation.Slowmo or TopazOperation.Stabilize && inputfile.Format != FileFormat.Mov)
                throw new TaskFailedException($"Cannot do {data.Operation.ToString().ToLowerInvariant()} on {inputfile.Format}");

            var ffprobe = await FFProbe.Get(inputfile.Path, Logger);

            if (data.Operation == TopazOperation.Slowmo)
                await Execute(inputfile.Path, data, ffprobe, outfiles.New(inputfile.Format).Path, AddSlowmoArgs);
            else if (data.Operation == TopazOperation.Upscale)
                await Execute(inputfile.Path, data, ffprobe, outfiles.New(inputfile.Format).Path, AddUpscaleArgs);
            else if (data.Operation == TopazOperation.Denoise)
                await Execute(inputfile.Path, data, ffprobe, outfiles.New(inputfile.Format).Path, AddDenoiseArgs);
            else if (data.Operation == TopazOperation.Stabilize)
                await Stabilize(inputfile.Path, data, ffprobe, outfiles.New(inputfile.Format).Path);
            else throw new TaskFailedException("Unknown operation");

            return output;
        }

        async Task Execute(string input, TopazInfo data, FFProbe.FFProbeInfo ffprobe, FFmpegLauncherOutput output, Action<TopazInfo, FFProbe.FFProbeInfo, FFmpegLauncher> argsAddFunc)
        {
            var ffmpeg = Path.Combine(PluginList.GetPlugin(PluginType.TopazVideoAI).Path, "../ffmpeg.exe");

            var launcher = new FFmpegLauncher(ffmpeg)
            {
                ILogger = Logger,
                ProgressSetter = ProgressSetter,
                EnvVariables =
                {
                    ["TVAI_MODEL_DIR"] = @"C:\ProgramData\Topaz Labs LLC\Topaz Video AI\models",
                    ["TVAI_MODEL_DATA_DIR"] = @"C:\ProgramData\Topaz Labs LLC\Topaz Video AI\models",
                },
                Input = { input },
                Outputs = { output },
            };

            argsAddFunc(data, ffprobe, launcher);
            await launcher.Execute();
        }
        async Task Execute(string input, TopazInfo data, FFProbe.FFProbeInfo ffprobe, string output, Action<TopazInfo, FFProbe.FFProbeInfo, FFmpegLauncher> argsAddFunc)
        {
            var ffoutput = new FFmpegLauncherOutput()
            {
                Codec = new ProresFFmpegCodec() { Profile = ProresFFmpegCodec.CopyProfileFrom(ffprobe.VideoStream) },
                Output = output,
                Args =
                {
                    // scaling flags
                    "-sws_flags", "spline+accurate_rnd+full_chroma_int",
                },
            };

            await Execute(input, data, ffprobe, ffoutput, argsAddFunc);
        }

        static void AddSlowmoArgs(TopazInfo data, FFProbe.FFProbeInfo ffprobe, FFmpegLauncher launcher)
        {
            var filter = new ArgList()
            {
                // slowmo filter with chronos model
                "tvai_fi=model=chr-1",

                // slowmo strength
                $"slowmo={(data.X ?? 2).ToString(NumberFormats.Normal)}",

                // no replacing duplicate frames
                "rdt=-0.000001",

                "vram=1", "instances=1",
            };

            launcher.VideoFilters.Add(new[] { string.Join(':', filter) });
        }
        static void AddUpscaleArgs(TopazInfo data, FFProbe.FFProbeInfo ffprobe, FFmpegLauncher launcher)
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
                $"scale=w={w.ToString(NumberFormats.Normal)}:h={h.ToString(NumberFormats.Normal)}:flags=lanczos:threads=0",

                $"scale=out_color_matrix=bt709",
            });
        }
        static void AddDenoiseArgs(TopazInfo data, FFProbe.FFProbeInfo ffprobe, FFmpegLauncher launcher)
        {
            var w = ffprobe.VideoStream.Width;
            var h = ffprobe.VideoStream.Height;

            var filter = new ArgList()
            {
                // artemis  (denoise/sharpen) filter with HQ input
                "tvai_up=model=ahq-12",

                "scale=0",

                // upscale size
                $"w={w.ToString(NumberFormats.Normal)}", $"h={h.ToString(NumberFormats.Normal)}",

                "vram=1", "instances=1",
            };

            launcher.VideoFilters.Add(new[]
            {
                string.Join(':', filter),

                $"scale=out_color_matrix=bt709",
            });
        }
        async Task Stabilize(string input, TopazInfo data, FFProbe.FFProbeInfo ffprobe, string output)
        {
            /*
            Stabilization needs two passes
            The first one gathers data and saves it to `jsonfile`
            The second one uses that data to stabilize
            */

            using var _ = Directories.DisposeDelete(Dirs.TempFile("topaz", extension: "json"), out var jsonfile);

            await Execute(input, data, ffprobe, new FFmpegLauncherOutput() { Codec = new NullFFmpegCodec(), Output = "-" }, addFirstPassArgs);
            await Execute(input, data, ffprobe, output, addSecondPassArgs);


            static string preprocessFilename(string filename) => filename.Replace(@"\", @"/").Replace(":", @"\\:");
            void addFirstPassArgs(TopazInfo data, FFProbe.FFProbeInfo ffprobe, FFmpegLauncher launcher)
            {
                var filter = new ArgList()
                {
                    // stabilization data collection filter
                    "tvai_cpe=model=cpe-1",

                    // stabilization data json result
                    $"filename={preprocessFilename(jsonfile)}",
                };

                launcher.VideoFilters.Add(new[] { string.Join(':', filter) });
            }
            void addSecondPassArgs(TopazInfo data, FFProbe.FFProbeInfo ffprobe, FFmpegLauncher launcher)
            {
                var filter = new ArgList()
                {
                    // stabilization filter
                    "tvai_stb=model=ref-2",

                    // stabilization data json input
                    $"filename={preprocessFilename(jsonfile)}",

                    // stabilization strength
                    $"smoothness={(data.Strength.GetValueOrDefault(.5f) * 12f).ToStringInvariant()}",

                    // full frame stabilization
                    "full=1",

                    // rolling shutter correction
                    "roll=0", 

                    // reduce jittering motion, number of passes
                    "reduce=0",

                    "rst=0", "wst=0", "cache=128", "dof=1111", "ws=32",

                    "vram=1", "instances=1",
                };

                launcher.VideoFilters.Add(new[] { string.Join(':', filter) });
            }
        }
    }
}
