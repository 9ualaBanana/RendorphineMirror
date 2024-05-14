namespace Node.Tasks.Exec.FFmpeg;


/*
TODO: copy metadata ?
-movflags use_metadata_tags
-map_metadata 0
-map_metadata:s:v 0:s:v
*/
public class FFmpegLauncher
{
    readonly string Executable;

    public MultiDictionary<string, string> EnvVariables { get; } = new();

    public InputList Input { get; } = new();
    public FilterList VideoFilters { get; } = new();
    public MultiList<string> AudioFilters { get; } = new();
    public MultiList<FFmpegLauncherOutput> Outputs { get; } = new();

    public required ILogger ILogger { get; init; }
    public IProgressSetter? ProgressSetter { get; init; }

    public FFmpegLauncher(string executable) => Executable = executable;


    /// <summary>
    /// Tries to copy input stream codec and parameters.
    /// </summary>
    /// <remarks>
    /// Parameters are different for every codec and we can't trust ffmpeg to keep the original quality so we need to copy parameters manually.
    /// H264 is the default fallback value.
    /// </remarks>
    public static IFFmpegCodec CodecFromStream(FFProbe.FFProbeStreamInfo stream) =>
        stream.CodecName switch
        {
            "prores" => new ProresFFmpegCodec() { Profile = ProresFFmpegCodec.CopyProfileFrom(stream) },
            "mjpeg" when stream.DurationTs != 1 => new H264NvencFFmpegCodec() { Bitrate = new BitrateData.Constant(stream.Bitrate) },

            var jpeg when jpeg.ContainsOrdinal("jpeg") || jpeg.ContainsOrdinal("jpg") =>
                new JpegFFmpegCodec(),

            "png" => new PngFFmpegCodec(),

            _ => new H264NvencFFmpegCodec() { Bitrate = new BitrateData.Constant(stream.Bitrate) },
        };


    public async Task Execute()
    {
        var fallbackCodec = false;
        var hwaccelInput = true;

        try
        {
            await launch();
        }
        catch (Exception ex) when (!fallbackCodec || Outputs.Any(p => p.Codec?.Fallback is not null))
        {
            ILogger.LogError($"{ex.Message}, restarting using {JsonConvert.SerializeObject(new { fallbackCodec, hwaccelInput })}..");
            await launch();
        }


        async Task launch()
        {
            using var _logscope = ILogger.BeginScope("FFmpeg");

            var args = new ArgList()
            {
                // hide useless info
                "-hide_banner",

                // enable hardware acceleration
                // but not for jpegs in fallback mode
                // because sometimes jpegs with hwaccel return `CUDA_ERROR_INVALID_IMAGE: device kernel image is invalid`
                hwaccelInput ? new[] { "-hwaccel", "auto" } : null,

                // force rewrite output file if exists
                "-y",

                Input.SelectMany(file => new ArgList() { file.Args, "-i", file.Path }),

                VideoFilters.Count == 0 ? null : new[] { "-filter_complex", string.Join(',', VideoFilters) },
                AudioFilters.Count == 0 ? null : new[] { "-af", string.Join(',', AudioFilters) },
                Outputs.SelectMany(p => p.Build(fallbackCodec)),
            };



            var duration = (await Task.WhenAll(Input.Select(f => FFProbe.Get(f.Path, ILogger)))).Select(ff => TimeSpan.FromSeconds(ff.Format.Duration)).Max();
            // if speed filter is active we should alter the duration for progress
            // but noone should change speed anyway and who cares about progress
            // duration /= argholder.Rate;

            await new ProcessLauncher(Executable, args)
            {
                ThrowOnStdErr = false,
                Logging = { ILogger = ILogger, StdErr = LogLevel.Trace },
                EnvVariables = { EnvVariables },
            }
                .AddOnRead(onRead)
                .ExecuteAsync();


            void onRead(bool err, string line)
            {
                if (line.Contains("10 bit encode not supported", StringComparison.Ordinal)
                    || line.Contains("No capable devices found", StringComparison.Ordinal)
                    || line.Contains("Cannot load nvcuda.dll", StringComparison.Ordinal)
                    || line.Contains("Driver does not support the required nvenc API version.", StringComparison.Ordinal)
                    || (line.Contains("h264_nvenc", StringComparison.Ordinal) && line.Contains("Cannot load libcuda.so", StringComparison.Ordinal)))
                {
                    fallbackCodec = true;
                    throw new Exception(line);
                }

                if (line.Contains("CUDA_ERROR_INVALID_IMAGE: device kernel image is invalid", StringComparison.Ordinal))
                {
                    hwaccelInput = false;
                    throw new Exception(line);
                }

                // frame=  502 fps=0.0 q=29.0 size=     256kB time=00:00:14.84 bitrate= 141.3kbits/s speed=29.5x
                if (!line.StartsWith("frame=", StringComparison.Ordinal)) return;

                var spt = line.AsSpan(line.IndexOf("time=", StringComparison.Ordinal) + "time=".Length).TrimStart();
                spt = spt.Slice(0, spt.IndexOf(' '));
                if (!TimeSpan.TryParse(spt, out var time))
                    time = TimeSpan.Zero;

                ProgressSetter?.Set(Math.Clamp(time / duration, 0, 1));
            }
        }
    }


    public class InputList : MultiList<FFmpegLauncherInput>
    {
        public void Add(string input) => base.Add(new FFmpegLauncherInput(input));
    }
    public class FilterList : MultiList<string>
    {
        public void Add(FFmpegFilter.FilterList list) => base.Add(list.Build());
    }
}
