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

public class FFMpegTasks : ProcessTaskExecutor<MediaEditInfo>
{
    public static readonly FFMpegTasks Instance = new();

    public readonly PluginAction<EditVideoInfo> EditVideo;
    public readonly PluginAction<EditRasterInfo> EditRaster;

    private FFMpegTasks()
    {
        EditVideo = new(PluginType.FFmpeg, nameof(EditVideo), FileFormat.Mov, Start);
        EditRaster = new(PluginType.FFmpeg, nameof(EditRaster), FileFormat.Jpeg, Start);
    }

    public override IEnumerable<IPluginAction> GetTasks() => new IPluginAction[] { EditVideo, EditRaster };

    protected override string GetArguments(TaskExecuteData task, MediaEditInfo data)
    {
        var vfilters = string.Join(',', data.ConstructFFMpegArguments());
        if (vfilters.Length == 0) throw new Exception("No vfilters specified in task");


        var args = "";

        // dont output useless info
        args += "-hide_banner ";

        // force rewrite output file if exists
        args += "-y ";

        // input file
        args += $"-i \"{task.Input}\" ";

        // video filters
        args += $"-vf \"{vfilters}\" ";

        // don't reencode audio
        args += $"-c:a copy ";

        // output path
        args += $" \"{task.Output}\" ";

        return args;
    }
    static async ValueTask<FFProbeInfo?> FFProbe(string[] files)
    {
        var ffprobe =
            File.Exists("/bin/ffprobe") ? "/bin/ffprobe"
            : File.Exists("assets/ffprobe.exe") ? "assets/ffprobe.exe"
            : null;

        if (ffprobe is null) return null;

        var proc = Process.Start(new ProcessStartInfo(ffprobe, $"-hide_banner -v quiet -print_format json -show_streams \"{files[0]}\"") { RedirectStandardOutput = true });
        if (proc is null) return null;

        var str = await proc.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        return JsonConvert.DeserializeObject<FFProbeInfo>(str);
    }


    record FFProbeInfo(ImmutableArray<FFProbeStreamInfo> Streams);
    record FFProbeStreamInfo(string CodecName, int Width, int Height);
}