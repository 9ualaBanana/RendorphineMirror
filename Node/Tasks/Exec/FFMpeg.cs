using System.Diagnostics;
using System.Globalization;
using Newtonsoft.Json;

namespace Node.Tasks.Exec;

public class FFMpegCrop
{
    public int X, Y, W, H;
}
public class FFMpegSpeed
{
    [JsonProperty("spd")]
    [Default(1d)]
    public double Speed;

    [JsonProperty("interp")]
    [Default(false)]
    public bool Interpolated;
}
public abstract class MediaEditInfo
{
    protected static readonly NumberFormatInfo NumberFormat = new()
    {
        NumberDecimalDigits = 2,
        NumberDecimalSeparator = ".",
        NumberGroupSeparator = string.Empty,
    };

    public FFMpegCrop? Crop;

    [Default(false)]
    public bool? Hflip;

    [Default(false)]
    public bool? Vflip;

    [JsonProperty("bri")]
    [Default(0), Ranged(-1, 1)]
    public double? Brightness;

    [JsonProperty("sat")]
    [Default(1), Ranged(0, 3)]
    public double? Saturation;

    [JsonProperty("con")]
    [Default(1), Ranged(-1000, 1000)]
    public double? Contrast;

    [JsonProperty("gam")]
    [Default(1), Ranged(.1, 10)]
    public double? Gamma;

    [JsonProperty("ro")]
    [Default(0), Ranged(-Math.PI * 2, Math.PI * 2)]
    public double? RotationRadians;


    public virtual void ConstructFFMpegArguments(FFMpegTasks.FFProbe.FFProbeInfo ffprobe, in FFMpegArgsHolder args, ref double rate)
    {
        if (Crop is not null) args.VideoFilters.Add($"crop={Crop.W.ToString(NumberFormat)}:{Crop.H.ToString(NumberFormat)}:{Crop.X.ToString(NumberFormat)}:{Crop.Y.ToString(NumberFormat)}");

        if (Hflip == true) args.VideoFilters.Add("hflip");
        if (Vflip == true) args.VideoFilters.Add("vflip");
        if (RotationRadians is not null) args.VideoFilters.Add($"rotate={RotationRadians.Value.ToString(NumberFormat)}");

        var eq = new List<string>();
        if (Brightness is not null) eq.Add($"brightness={Brightness.Value.ToString(NumberFormat)}");
        if (Saturation is not null) eq.Add($"saturation={Saturation.Value.ToString(NumberFormat)}");
        if (Contrast is not null) eq.Add($"contrast={Contrast.Value.ToString(NumberFormat)}");
        if (Gamma is not null) eq.Add($"gamma={Gamma.Value.ToString(NumberFormat)}");

        if (eq.Count != 0) args.VideoFilters.Add($"eq={string.Join(':', eq)}");
    }
}
public class EditVideoInfo : MediaEditInfo
{
    [JsonProperty("spd")]
    public FFMpegSpeed? Speed;

    [JsonProperty("startFrame")]
    [Default(0d)]
    public double? StartFrame;

    [JsonProperty("endFrame")]
    public double? EndFrame;

    public override void ConstructFFMpegArguments(FFMpegTasks.FFProbe.FFProbeInfo ffprobe, in FFMpegArgsHolder args, ref double rate)
    {
        base.ConstructFFMpegArguments(ffprobe, args, ref rate);

        if (Speed is not null)
        {
            rate = Speed.Speed;
            args.VideoFilters.Add($"setpts={(1d / Speed.Speed).ToString(NumberFormat)}*PTS");
            args.AudioFilers.Add($"atempo={Speed.Speed.ToString(NumberFormat)}");

            var fps = ffprobe.VideoStream.FrameRate;
            args.Args.Add("-r", Math.Max(fps, (fps * Speed.Speed)).ToString(NumberFormat));
            if (Speed.Interpolated) args.VideoFilters.Add($"minterpolate='mi_mode=mci:mc_mode=aobmc:vsbmc=1:fps={Math.Max(fps, (fps * Speed.Speed)).ToString(NumberFormat)}'");
        }

        if (EndFrame is not null && StartFrame is not null) args.VideoFilters.Add($"trim=start_frame={StartFrame.Value.ToString(NumberFormat)}:end_frame={EndFrame.Value.ToString(NumberFormat)}");
    }
}
public class EditRasterInfo : MediaEditInfo { }

public class FFMpegQSPreviewInfo { }

public readonly struct FFMpegArgsHolder
{
    public readonly ArgList Args;
    public readonly List<string> VideoFilters, AudioFilers, Filtergraph;

    public FFMpegArgsHolder(ArgList args, List<string> videoFilters, List<string> audioFilers, List<string> filtergraph)
    {
        Args = args;
        VideoFilters = videoFilters;
        AudioFilers = audioFilers;
        Filtergraph = filtergraph;
    }
}
public static class FFMpegTasks
{
    public static IEnumerable<IPluginAction> CreateTasks() => new IPluginAction[] { new FFMpegEditVideo(), new FFMpegEditRaster() };


    abstract class FFMpegAction<T> : InputOutputPluginAction<T>
    {
        public override PluginType Type => PluginType.FFmpeg;

        protected sealed override async Task ExecuteImpl(ReceivedTask task, T data)
        {
            var inputfile = task.FSInputFile();
            var outputfile = task.FSNewOutputFile(InputFileFormat.ToString().ToLowerInvariant());

            var ffprobe = await FFProbe.Get(inputfile, task) ?? throw new Exception();


            var exepath = task.GetPlugin().GetInstance().Path;

            var rate = 1d;
            var args = getArgs(ref rate);

            var duration = TimeSpan.FromSeconds(ffprobe.Format.Duration);
            task.LogInfo($"{inputfile} duration: {duration} x{rate}");
            duration /= rate;

            await ExecuteProcess(exepath, args, true, onRead, task);


            void onRead(bool err, string line)
            {
                // frame=  502 fps=0.0 q=29.0 size=     256kB time=00:00:14.84 bitrate= 141.3kbits/s speed=29.5x
                if (!line.StartsWith("frame=")) return;

                var spt = line.AsSpan(line.IndexOf("time=", StringComparison.Ordinal) + "time=".Length).TrimStart();
                spt = spt.Slice(0, spt.IndexOf(' '));
                var time = TimeSpan.Parse(spt);

                task.Progress = Math.Clamp(time / duration, 0, 1);
                NodeGlobalState.Instance.ExecutingTasks.TriggerValueChanged();
            }
            IEnumerable<string> getArgs(ref double rate)
            {
                var argsarr = new ArgList();
                var videofilters = new List<string>();
                var audiofilters = new List<string>();
                var filtergraph = new List<string>();

                ConstructFFMpegArguments(data, ffprobe, new(argsarr, videofilters, audiofilters, filtergraph), ref rate);
                if (videofilters.Count != 0 && filtergraph.Count != 0)
                    throw new Exception("Video filters and filtergraph could not be used together");

                return new ArgList()
                {
                    // hide useless info
                    "-hide_banner",

                    // enable hardware acceleration if video
                    (data is EditVideoInfo ? new[] { "-hwaccel", "auto" } : null ),

                    // force rewrite output file if exists
                    "-y",
                    // input file
                    "-i", inputfile,

                    // arguments
                    argsarr,

                    // video filters
                    iffilter(() => videofilters.Count == 0, "-vf", videofilters),
                    // audio filters
                    iffilter(() => audiofilters.Count == 0, "-af", audiofilters),
                    // complex filters
                    iffilter(() => filtergraph.Count == 0, "-filter_complex", audiofilters),

                    // output path
                    outputfile,
                };


                static string[]? iffilter(Func<bool> action, string arg, IEnumerable<string> filters) => action() ? new[] { arg, string.Join(',', filters) } : null;
            }
        }

        protected abstract void ConstructFFMpegArguments(T data, FFMpegTasks.FFProbe.FFProbeInfo ffprobe, in FFMpegArgsHolder args, ref double rate);
    }
    abstract class FFMpegMediaEditAction<T> : FFMpegAction<T> where T : MediaEditInfo
    {
        protected override void ConstructFFMpegArguments(T data, FFProbe.FFProbeInfo ffprobe, in FFMpegArgsHolder args, ref double rate) =>
            data.ConstructFFMpegArguments(ffprobe, args, ref rate);
    }
    class FFMpegEditVideo : FFMpegMediaEditAction<EditVideoInfo>
    {
        public override string Name => "EditVideo";
        public override FileFormat InputFileFormat => FileFormat.Mov;
    }
    class FFMpegEditRaster : FFMpegMediaEditAction<EditRasterInfo>
    {
        public override string Name => "EditRaster";
        public override FileFormat InputFileFormat => FileFormat.Jpeg;
    }
    class FFMpegQSPreview : FFMpegAction<FFMpegQSPreviewInfo>
    {
        public override string Name => "QSPreview";
        public override FileFormat InputFileFormat => FileFormat.Jpeg;

        protected override void ConstructFFMpegArguments(FFMpegQSPreviewInfo data, FFProbe.FFProbeInfo ffprobe, in FFMpegArgsHolder args, ref double rate)
        {
            var graph = "";

            // repeat watermark several times vertically and horizontally
            graph += "[1][1] hstack, split, vstack," + string.Join(string.Empty, Enumerable.Repeat("split, hstack, split, vstack,", 2));

            // rotate watermark -20 deg
            graph += "rotate= -20*PI/180:fillcolor=none:ow=rotw(iw):oh=roth(ih),";

            // add watermark onto the base video/image
            graph += "[0] overlay= (main_w-overlay_w)/2:(main_h-overlay_h)/2:format=auto,";

            // scale everything to 640px by width
            graph += "scale= w=in_w/in_h*640:h=640,";

            // set the color format
            graph += "format= yuv420p";

            args.Filtergraph.Add(graph);
        }
    }


    public static class FFProbe
    {
        public static async Task<FFProbeInfo> Get(string file, ILoggable? logobj)
        {
            var ffprobe = File.Exists("/bin/ffprobe") ? "/bin/ffprobe" : "assets/ffprobe.exe";

            var args = $"-hide_banner -v quiet -print_format json -show_streams -show_format \"{file}\"";
            logobj?.LogInfo($"Starting {ffprobe} {args}");


            var proc = Process.Start(new ProcessStartInfo(ffprobe, args) { RedirectStandardOutput = true });
            if (proc is null) throw new Exception("Could not start ffprobe");

            var str = await proc.StandardOutput.ReadToEndAsync();
            return JsonConvert.DeserializeObject<FFProbeInfo>(str) ?? throw new Exception($"Could not parse ffprobe output: {str}");
        }


        public enum FFProbeCodecType { Unknown, Video, Audio }
        public record FFProbeInfo(ImmutableArray<FFProbeStreamInfo> Streams, FFProbeFormatInfo Format)
        {
            // die if there are multiple video streams
            public FFProbeStreamInfo VideoStream => Streams.Single(x => x.CodecType == FFProbeCodecType.Video);
        };

        public record FFProbeStreamInfo(
            int Width,
            int Height,
            [JsonProperty("codec_name")] string CodecName,
            [JsonProperty("codec_type")] FFProbeCodecType CodecType,
            [JsonProperty("r_frame_rate")] string FrameRateString
        )
        {
            public double FrameRate => double.Parse(FrameRateString.Split('/')[0]) / double.Parse(FrameRateString.Split('/')[1]);
        }

        public record FFProbeFormatInfo(
            double Duration
        );
    }
}