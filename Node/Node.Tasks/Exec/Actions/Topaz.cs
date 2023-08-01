namespace Node.Tasks.Exec.Actions;

public class Topaz : FilePluginAction<TopazInfo>
{
    public override TaskAction Name => TaskAction.Topaz;
    public override ImmutableArray<PluginType> RequiredPlugins => ImmutableArray.Create(PluginType.TopazVideoAI);

    public override IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats =>
        new[] { new[] { FileFormat.Mov } };

    protected override OperationResult ValidateOutputFiles(TaskFilesCheckData files, TopazInfo data) =>
        files.EnsureSingleInputFile()
        .Next(input => files.EnsureSingleOutputFile()
        .Next(output => TaskRequirement.EnsureSameFormat(output, input)));

    public override async Task<TaskFileOutput> ExecuteUnchecked(TaskFileInput input, TopazInfo data)
    {
        var ffmpeg = Path.Combine(PluginList.GetPlugin(PluginType.TopazVideoAI).Path, "../ffmpeg.exe");

        var output = new TaskFileOutput(input.ResultDirectory);
        var outfiles = output.Files.New();
        foreach (var file in input)
        {
            var ffprobe = await FFProbe.Get(file.Path, Logger);

            var launcher = new FFmpegLauncher(ffmpeg)
            {
                ILogger = Logger,
                ProgressSetter = ProgressSetter,
                EnvVariables =
                {
                    ["TVAI_MODEL_DIR"] = @"C:\ProgramData\Topaz Labs LLC\Topaz Video AI\models",
                    ["TVAI_MODEL_DATA_DIR"] = @"C:\ProgramData\Topaz Labs LLC\Topaz Video AI\models",
                },
                Input = { file.Path },
                Outputs =
                {
                    new FFmpegLauncherOutput()
                    {
                        Codec = new ProresFFmpegCodec() { Profile = ProresFFmpegCodec.CopyProfileFrom(ffprobe.VideoStream) },
                        Output = outfiles.New(file.Format).Path,
                        Args =
                        {
                            // scaling flags
                            "-sws_flags", "spline+accurate_rnd+full_chroma_int",
                        },
                    },
                },
            };

            AddFilters(data, ffprobe, launcher);
            await launcher.Execute();
        }

        return output;
    }


    static void AddFilters(TopazInfo data, FFProbe.FFProbeInfo ffprobe, FFmpegLauncher launcher)
    {
        if (data.Operation == TopazOperation.Slowmo)
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

            launcher.VideoFilters.Add(new[]
            {
                string.Join(':', filter),
            });
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
                $"scale=w={w.ToString(NumberFormats.Normal)}:h={h.ToString(NumberFormats.Normal)}:flags=lanczos:threads=0",

                $"scale=out_color_matrix=bt709",
            });
        }
        else if (data.Operation == TopazOperation.Denoise)
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

                // ffmpeg "-hide_banner" "-nostdin" "-y" "-nostats" "-i" "C:/Users/user/Documents/mov.mov" "-sws_flags" "spline+accurate_rnd+full_chroma_int" "-color_trc" "2" "-colorspace" "5" "-color_primaries" "2" "-filter_complex" "tvai_up=model=prob-3:scale=0:w=3840:h=2160:preblur=0:noise=0:details=0:halo=0:blur=0:compression=0:estimate=20:device=0:vram=1:instances=1,scale=w=3840:h=2160:flags=lanczos:threads=0,scale=out_color_matrix=bt709" "-c:v" "prores_ks" "-profile:v" "1" "-vendor" "apl0" "-quant_mat" "lt" "-bits_per_mb" "525" "-pix_fmt" "yuv422p10le" "-map_metadata" "0" "-movflags" "frag_keyframe+empty_moov+delay_moov+use_metadata_tags+write_colr " "-map_metadata:s:v" "0:s:v" "-an" "-metadata" "videoai=Enhanced using prob-3 auto with recover details at 0, dehalo at 0, reduce noise at 0, sharpen at 0, revert compression at 0, and anti-alias/deblur at 0. Changed resolution to 3840x2160" "C:/Users/user/Documents/mov_1_prob3_temp.mov"
                $"scale=out_color_matrix=bt709",
            });
        }
        else throw new TaskFailedException("Unknown operation");
    }
}
