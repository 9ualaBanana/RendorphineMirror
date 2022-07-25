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
    public bool? Hflip = false;
    public bool? Vflip = false;

    [JsonProperty("bri")]
    [Ranged(-1, 1)]
    public double? Brightness = 0;

    [JsonProperty("sat")]
    [Ranged(0, 3)]
    public double? Saturation = 1;

    [JsonProperty("con")]
    [Ranged(-1000, 1000)]
    public double? Contrast = 1;

    [JsonProperty("gam")]
    [Ranged(.1, 10)]
    public double? Gamma = 1;

    [JsonProperty("ro")]
    [Ranged(-Math.PI * 2, Math.PI * 2)]
    public double? RotationRadians = 0;


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
    public double? CutFrameAt;

    public override IEnumerable<string> ConstructFFMpegArguments()
    {
        var args = base.ConstructFFMpegArguments();
        foreach (var arg in args)
            yield return arg;

        if (CutFrameAt is not null) yield return $"trim=start_frame=0:end_frame={CutFrameAt.Value.ToString(NumberFormat)}";
    }
}
public class EditRasterInfo : MediaEditInfo { }

public static class FFMpegTasks
{
    public static IEnumerable<IPluginAction> Create()
    {
        yield return new PluginAction<EditVideoInfo>(PluginType.FFmpeg, "EditVideo", FileFormat.Mov, Start);
        yield return new PluginAction<EditRasterInfo>(PluginType.FFmpeg, "EditRaster", FileFormat.Jpeg, Start);
    }
    static async ValueTask<string[]> Start<T>(string[] files, ReceivedTask task, T data) where T : MediaEditInfo =>
        await Task.WhenAll(files.Select(x => Execute(x, task, data))).ConfigureAwait(false);

    static async Task<string> Execute<T>(string input, ReceivedTask task, T data) where T : MediaEditInfo
    {
        var output = Path.Combine(Init.TaskFilesDirectory, task.Id, Path.GetFileNameWithoutExtension(input) + "_out" + Path.GetExtension(input));
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);

        var args = "";

        // dont output useless info
        args += "-hide_banner ";

        // force rewrite output file if exists
        args += "-y ";

        // input file
        args += $"-i \"{input}\" ";

        // filters
        args += $"-vf \"{string.Join(',', data.ConstructFFMpegArguments())}\" ";

        // don't reencode audio
        args += $"-c:a copy ";

        // output path
        args += $" \"{output}\" ";


        // TODO: fix getting path
        var exepath = File.Exists("/bin/ffmpeg") ? "/bin/ffmpeg" : "assets/ffmpeg.exe";

        task.LogInfo($"Starting {exepath} {args}");

        var process = Process.Start(new ProcessStartInfo(exepath, args));
        if (process is null) throw new InvalidOperationException("Could not start plugin process");

        await process.WaitForExitAsync().ConfigureAwait(false);
        var ret = process.ExitCode;
        if (ret != 0) throw new Exception("Could not complete task fo some reason");

        task.LogInfo($"Completed ffmpeg execution");
        return output;
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