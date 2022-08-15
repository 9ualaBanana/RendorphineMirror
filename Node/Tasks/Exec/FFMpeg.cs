using System.Diagnostics;
using System.Globalization;
using Newtonsoft.Json;

namespace Node.Tasks.Exec;

public class Crop
{
    public int X, Y, W, H;
}
public abstract class MediaEditInfo
{
    protected static readonly NumberFormatInfo NumberFormat = new()
    {
        NumberDecimalDigits = 2,
        NumberDecimalSeparator = ".",
        NumberGroupSeparator = string.Empty,
    };

    public Crop? Crop;

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


    public virtual IEnumerable<string> ConstructFFMpegArguments()
    {
        if (Crop is not null) yield return $"crop={Crop.W.ToString(NumberFormat)}:{Crop.H.ToString(NumberFormat)}:{Crop.X.ToString(NumberFormat)}:{Crop.Y.ToString(NumberFormat)}";

        if (Hflip == true) yield return "hflip";
        if (Vflip == true) yield return "vflip";
        if (RotationRadians is not null) yield return $"rotate={RotationRadians.Value.ToString(NumberFormat)}";

        var eq = new List<string>();
        if (Brightness is not null) eq.Add($"brightness={Brightness.Value.ToString(NumberFormat)}");
        if (Saturation is not null) eq.Add($"saturation={Saturation.Value.ToString(NumberFormat)}");
        if (Contrast is not null) eq.Add($"contrast={Contrast.Value.ToString(NumberFormat)}");
        if (Gamma is not null) eq.Add($"gamma={Gamma.Value.ToString(NumberFormat)}");

        if (eq.Count != 0) yield return $"eq={string.Join(':', eq)}";
    }
}
public class EditVideoInfo : MediaEditInfo
{
    [JsonProperty("startFrame")]
    [Default(0d)]
    public double? StartFrame;

    [JsonProperty("endFrame")]
    public double? EndFrame;

    public override IEnumerable<string> ConstructFFMpegArguments()
    {
        var args = base.ConstructFFMpegArguments();
        foreach (var arg in args)
            yield return arg;

        if (EndFrame is not null && StartFrame is not null) yield return $"trim=start_frame={StartFrame.Value.ToString(NumberFormat)}:end_frame={EndFrame.Value.ToString(NumberFormat)}";
    }
}
public class EditRasterInfo : MediaEditInfo { }

public static class FFMpegTasks
{
    public static IEnumerable<IPluginAction> CreateTasks() => new IPluginAction[] { new FFMpegEditVideo(), new FFMpegEditRaster() };


    abstract class FFMpegEditAction<T> : PluginAction<T> where T : MediaEditInfo
    {
        public override PluginType Type => PluginType.FFmpeg;

        protected override async Task<string> Execute(ReceivedTask task, T data)
        {
            task.InputFile.ThrowIfNull();
            var frames = (await FFProbe.Get(task.InputFile, task))?.Streams.FirstOrDefault()?.Frames ?? 0;
            task.LogInfo($"{task.InputFile} length: {frames} frames");

            var output = GetTaskOutputFile(task);
            var exepath = task.Plugin.GetInstance().Path;
            var args = getArgs();

            await ExecuteProcess(exepath, args, true, onRead, task);
            return output;


            void onRead(bool err, string line)
            {
                // frame=  502 fps=0.0 q=29.0 size=     256kB time=00:00:14.84 bitrate= 141.3kbits/s speed=29.5x
                if (!line.StartsWith("frame=")) return;

                var spt = line.AsSpan("frame=".Length).TrimStart();
                spt = spt.Slice(0, spt.IndexOf(' '));
                var frame = ulong.Parse(spt);

                task.Progress = Math.Clamp((double) frame / frames, 0, 1);
                NodeGlobalState.Instance.ExecutingTasks.TriggerValueChanged();
            }
            string getArgs()
            {
                var vfilters = string.Join(',', data.ConstructFFMpegArguments());
                if (vfilters.Length == 0) throw new Exception("No vfilters specified in task");


                var args = "";

                // dont output useless info
                args += "-hide_banner ";

                // force rewrite output file if exists
                args += "-y ";

                // input file
                args += $"-i \"{task.InputFile}\" ";

                // video filters
                args += $"-vf \"{vfilters}\" ";

                // don't reencode audio
                args += $"-c:a copy ";

                // output path
                args += $" \"{output}\" ";

                return args;
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


    static class FFProbe
    {
        public static async Task<FFProbeInfo?> Get(string file, ILoggable? logobj)
        {
            var ffprobe =
                File.Exists("/bin/ffprobe") ? "/bin/ffprobe"
                : File.Exists("assets/ffprobe.exe") ? "assets/ffprobe.exe"
                : null;

            if (ffprobe is null) return null;

            var args = $"-hide_banner -v quiet -print_format json -show_streams -count_frames \"{file}\"";
            logobj?.LogInfo($"Starting {args} {args}");


            var proc = Process.Start(new ProcessStartInfo(ffprobe, args) { RedirectStandardOutput = true });
            if (proc is null) return null;

            var str = await proc.StandardOutput.ReadToEndAsync();
            return JsonConvert.DeserializeObject<FFProbeInfo>(str);
        }

        public record FFProbeInfo(ImmutableArray<FFProbeStreamInfo> Streams);
        public record FFProbeStreamInfo([JsonProperty("codec_name")] string CodecName, int Width, int Height, [JsonProperty("nb_read_frames")] ulong Frames);
    }
}