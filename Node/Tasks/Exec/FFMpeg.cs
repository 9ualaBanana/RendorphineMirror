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


    public virtual void ConstructFFMpegArguments(FFMpegTasks.FFProbe.FFProbeInfo ffprobe, ArgList args, List<string> filters, ref double rate)
    {
        if (Crop is not null) filters.Add($"crop={Crop.W.ToString(NumberFormat)}:{Crop.H.ToString(NumberFormat)}:{Crop.X.ToString(NumberFormat)}:{Crop.Y.ToString(NumberFormat)}");

        if (Hflip == true) filters.Add("hflip");
        if (Vflip == true) filters.Add("vflip");
        if (RotationRadians is not null) filters.Add($"rotate={RotationRadians.Value.ToString(NumberFormat)}");

        var eq = new List<string>();
        if (Brightness is not null) eq.Add($"brightness={Brightness.Value.ToString(NumberFormat)}");
        if (Saturation is not null) eq.Add($"saturation={Saturation.Value.ToString(NumberFormat)}");
        if (Contrast is not null) eq.Add($"contrast={Contrast.Value.ToString(NumberFormat)}");
        if (Gamma is not null) eq.Add($"gamma={Gamma.Value.ToString(NumberFormat)}");

        if (eq.Count != 0) filters.Add($"eq={string.Join(':', eq)}");
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

    public override void ConstructFFMpegArguments(FFMpegTasks.FFProbe.FFProbeInfo ffprobe, ArgList args, List<string> filters, ref double rate)
    {
        base.ConstructFFMpegArguments(ffprobe, args, filters, ref rate);

        if (Speed is not null)
        {
            rate = Speed.Speed;
            filters.Add($"setpts={(1d / Speed.Speed).ToString(NumberFormat)}*PTS");

            var fps = ffprobe.VideoStream.FrameRate;
            args.Add("-r", Math.Max(fps, (fps * Speed.Speed)).ToString(NumberFormat));
            if (Speed.Interpolated) filters.Add($"minterpolate='mi_mode=mci:mc_mode=aobmc:vsbmc=1:fps={Math.Max(fps, (fps * Speed.Speed)).ToString(NumberFormat)}'");
        }

        if (EndFrame is not null && StartFrame is not null) filters.Add($"trim=start_frame={StartFrame.Value.ToString(NumberFormat)}:end_frame={EndFrame.Value.ToString(NumberFormat)}");
    }
}
public class EditRasterInfo : MediaEditInfo { }

public static class FFMpegTasks
{
    public static IEnumerable<IPluginAction> CreateTasks() => new IPluginAction[] { new FFMpegEditVideo(), new FFMpegEditRaster() };


    abstract class FFMpegEditAction<T> : InputOutputPluginAction<T> where T : MediaEditInfo
    {
        public override PluginType Type => PluginType.FFmpeg;

        protected override async Task Execute(ReceivedTask task, T data, ITaskInput input, ITaskOutput output)
        {
            var outputfile = task.FSOutputFile();

            var ffprobe = await FFProbe.Get(task.InputFile, task) ?? throw new Exception();


            var exepath = task.GetPlugin().GetInstance().Path;

            var rate = 1d;
            var args = getArgs(ref rate);

            var duration = TimeSpan.FromSeconds(ffprobe.Format.Duration);
            task.LogInfo($"{task.InputFile} duration: {duration} x{rate}");
            duration /= rate;

            await ExecuteProcess(exepath, args, true, onRead, task);
            await UploadResult(task, output, outputfile);


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
                var filtersarr = new List<string>();

                data.ConstructFFMpegArguments(ffprobe, argsarr, filtersarr, ref rate);
                if (filtersarr.Count == 0) throw new Exception("No vfilters specified in task");

                return new ArgList()
                {
                    "-hide_banner",                         // dont output useless info
                    "-hwaccel", "auto",                     // enable hardware acceleration
                    "-y",                                   // force rewrite output file if exists
                    "-i", task.InputFile,                   // input file

                    argsarr,

                    "-vf", string.Join(',', filtersarr),    // video filters
                    "-c:a", "copy",                         // don't reencode audio
                    outputfile,                             // output path
                };
            }
        }
    }
    class FFMpegEditVideo : FFMpegEditAction<EditVideoInfo>
    {
        public override string Name => "EditVideo";
        public override FileFormat FileFormat => FileFormat.Mov;
    }
    class FFMpegEditRaster : FFMpegEditAction<EditRasterInfo>
    {
        public override string Name => "EditRaster";
        public override FileFormat FileFormat => FileFormat.Jpeg;
    }


    public static class FFProbe
    {
        public static async Task<FFProbeInfo> Get(string file, ILoggable? logobj)
        {
            var ffprobe = File.Exists("/bin/ffprobe") ? "/bin/ffprobe" : "assets/ffprobe.exe";

            // TODO: dont count frames, too long for some videos
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