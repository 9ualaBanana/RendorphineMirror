using System.Collections;
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

    [JsonProperty("wat")]
    [Default("qs_watermark.png")]
    public string? Watermark;
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
}
public class EditRasterInfo : MediaEditInfo { }

public class FFMpegArgsHolder
{
    public readonly FFMpegTasks.FFProbe.FFProbeInfo? FFProbe;
    public double Rate = 1;

    public readonly ArgList Args = new();
    public readonly OrderList<string> AudioFilers = new();
    public readonly OrderList<string> Filtergraph = new();

    public FFMpegArgsHolder(FFMpegTasks.FFProbe.FFProbeInfo? ffprobe) => FFProbe = ffprobe;



    public class OrderList<T> : IEnumerable<T>
    {
        public int Count => Items.Count + ItemsLast.Count;

        readonly List<T> Items = new();
        readonly List<T> ItemsLast = new();

        public void AddFirst(T item) => Items.Insert(0, item);
        public void Add(T item) => Items.Add(item);
        public void AddLast(T item) => ItemsLast.Add(item);

        public IEnumerator<T> GetEnumerator() => Items.Concat(ItemsLast).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
public static class FFMpegTasks
{
    public static IEnumerable<IPluginAction> CreateTasks() => new IPluginAction[] { new FFMpegEditVideo(), new FFMpegEditRaster() };


    abstract class FFMpegAction<T> : InputOutputPluginAction<T>
    {
        protected static readonly NumberFormatInfo NumberFormat = new()
        {
            NumberDecimalDigits = 2,
            NumberDecimalSeparator = ".",
            NumberGroupSeparator = string.Empty,
        };

        public override PluginType Type => PluginType.FFmpeg;

        protected sealed override async Task ExecuteImpl(ReceivedTask task, T data)
        {
            var inputfile = task.FSInputFile();
            var outputfile = task.FSNewOutputFile(InputFileFormat.ToString().ToLowerInvariant());

            var ffprobe = await FFProbe.Get(inputfile, task) ?? throw new Exception();


            var exepath = task.GetPlugin().GetInstance().Path;

            var rate = 1d;

            var argholder = new FFMpegArgsHolder(ffprobe);
            ConstructFFMpegArguments(task, data, argholder);
            var args = GetFFMpegArgs(inputfile, outputfile, task, data, argholder);

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
        }

        protected IEnumerable<string> GetFFMpegArgs(string inputfile, string outputfile, ReceivedTask task, T data, FFMpegArgsHolder argholder)
        {
            var argsarr = argholder.Args;
            var audiofilters = argholder.AudioFilers;
            var filtergraph = argholder.Filtergraph;

            return new ArgList()
            {
                // hide useless info
                "-hide_banner",

                // enable hardware acceleration if video
                (data is EditVideoInfo ? new[] { "-hwaccel", "auto", "-threads", "1" } : null ),

                // force rewrite output file if exists
                "-y",
                // input file
                "-i", inputfile,

                // arguments
                argsarr,

                // video filters
                filtergraph.Count == 0 ? null : new[] { "-filter_complex", string.Join(',', filtergraph) },

                // audio filters
                audiofilters.Count == 0 ? null : new[] { "-af", string.Join(',', audiofilters) },

                // output path
                outputfile,
            };
        }


        protected abstract void ConstructFFMpegArguments(ReceivedTask task, T data, in FFMpegArgsHolder args);
    }

    abstract class FFMpegMediaEditAction<T> : FFMpegAction<T> where T : MediaEditInfo
    {
        protected override void ConstructFFMpegArguments(ReceivedTask task, T data, in FFMpegArgsHolder args)
        {
            var filters = args.Filtergraph;

            var eq = new List<string>();
            if (data.Brightness is not null) eq.Add($"brightness={data.Brightness.Value.ToString(NumberFormat)}");
            if (data.Saturation is not null) eq.Add($"saturation={data.Saturation.Value.ToString(NumberFormat)}");
            if (data.Contrast is not null) eq.Add($"contrast={data.Contrast.Value.ToString(NumberFormat)}");
            if (data.Gamma is not null) eq.Add($"gamma={data.Gamma.Value.ToString(NumberFormat)}");
            if (eq.Count != 0) filters.Add($"eq={string.Join(':', eq)}");


            if (data.Hflip == true) filters.Add("hflip");
            if (data.Vflip == true) filters.Add("vflip");
            if (data.RotationRadians is not null) filters.Add($"rotate={data.RotationRadians.Value.ToString(NumberFormat)}");

            if (data.Crop is not null) filters.AddFirst($"crop={data.Crop.W.ToString(NumberFormat)}:{data.Crop.H.ToString(NumberFormat)}:{data.Crop.X.ToString(NumberFormat)}:{data.Crop.Y.ToString(NumberFormat)}");

            if (data.Watermark is not null)
            {
                var watermarkFile = Path.GetFullPath(Path.Combine("assets", data.Watermark));
                if (!watermarkFile.StartsWith(Path.GetFullPath("assets"), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Invalid watermark file path: {watermarkFile}");

                watermarkFile = createWatermark().GetAwaiter().GetResult();
                async Task<string> createWatermark()
                {
                    var graph = "";

                    // repeat watermark several times vertically and horizontally
                    graph += "[0][0] hstack, split, vstack," + string.Join(string.Empty, Enumerable.Repeat("split, hstack, split, vstack,", 2));

                    // rotate watermark -20 deg
                    graph += "rotate= -20*PI/180:fillcolor=none:ow=rotw(iw):oh=roth(ih), format= rgba";

                    var exepath = task.GetPlugin().GetInstance().Path;
                    var tempf = task.GetTempFileName(Path.GetExtension(watermarkFile));

                    var argholder = new FFMpegArgsHolder(null);
                    argholder.Filtergraph.Add(graph);

                    var ffargs = GetFFMpegArgs(watermarkFile, tempf, task, data, argholder);
                    await ExecuteProcess(exepath, ffargs, true, delegate { }, task);

                    return tempf;
                }


                args.Args.Add("-i", watermarkFile);
                var graph = "";

                // scale video to 640px by width
                graph += "scale= w=mod(in_w/in_h*640\\,2)+in_w/in_h*640:h=640 [v];";

                // add watermark onto the base video/image
                graph += "[v][1] overlay= (main_w-overlay_w)/2:(main_h-overlay_h)/2:format=auto,";

                // set the color format
                graph += "format= yuv420p";

                args.Filtergraph.AddLast(graph);
            }
        }
    }
    class FFMpegEditVideo : FFMpegMediaEditAction<EditVideoInfo>
    {
        public override string Name => "EditVideo";
        public override FileFormat InputFileFormat => FileFormat.Mov;

        protected override void ConstructFFMpegArguments(ReceivedTask task, EditVideoInfo data, in FFMpegArgsHolder args)
        {
            var filters = args.Filtergraph;
            base.ConstructFFMpegArguments(task, data, args);

            if (data.Speed is not null)
            {
                args.Rate = data.Speed.Speed;
                filters.Add($"setpts={(1d / data.Speed.Speed).ToString(NumberFormat)}*PTS");
                args.AudioFilers.Add($"atempo={data.Speed.Speed.ToString(NumberFormat)}");

                var fps = args.FFProbe?.VideoStream.FrameRate ?? 60;
                args.Args.Add("-r", Math.Max(fps, (fps * data.Speed.Speed)).ToString(NumberFormat));
                if (data.Speed.Interpolated) filters.Add($"minterpolate='mi_mode=mci:mc_mode=aobmc:vsbmc=1:fps={Math.Max(fps, (fps * data.Speed.Speed)).ToString(NumberFormat)}'");
            }

            var trim = new List<string>();
            if (data.StartFrame is not null) trim.Add($"start_frame={data.StartFrame.Value.ToString(NumberFormat)}");
            if (data.EndFrame is not null) trim.Add($"end_frame={data.EndFrame.Value.ToString(NumberFormat)}");
            if (trim.Count != 0) filters.AddFirst($"trim={string.Join(';', trim)}");
        }
    }
    class FFMpegEditRaster : FFMpegMediaEditAction<EditRasterInfo>
    {
        public override string Name => "EditRaster";
        public override FileFormat InputFileFormat => FileFormat.Jpeg;
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