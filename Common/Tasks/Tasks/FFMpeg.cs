using System.Diagnostics;
using Common.Tasks.Models;
using Newtonsoft.Json;

namespace Common.Tasks.Tasks;

public class Crop
{
    public int X, Y, W, H;
}
public abstract class MediaEditInfo : IPluginActionData
{
    public abstract string Type { get; }

    public Crop? Crop;
    public double? Bri, Sat, Con, Gam;
    public bool? Hflip, Vflip;
    public double? Ro;


    public virtual string ConstructFFMpegArguments()
    {
        var filters = new List<string>();

        if (Crop is not null) filters.Add($"\"crop={Crop.W}:{Crop.H}:{Crop.X}:{Crop.Y}\"");

        // TODO:

        if (Hflip == true) filters.Add("hflip");
        if (Vflip == true) filters.Add("vflip");

        return string.Join(' ', filters.Select(x => "-vf " + x));
    }
}
public class EditVideoInfo : MediaEditInfo
{
    public override string Type => "EditVideo";
    public double? CutFrameAt;

    public override string ConstructFFMpegArguments()
    {
        var args = base.ConstructFFMpegArguments();

        // TODO:

        return args;
    }
}
public class EditRasterInfo : MediaEditInfo
{
    public override string Type => "EditRaster";
}

public static class FFMpegTasks
{
    public static IEnumerable<IPluginAction> Create()
    {
        yield return new PluginAction<EditVideoInfo>(
            PluginType.FFmpeg,
            "EditVideo",
            createmedia<EditVideoInfo>,
            start
        );
        yield return new PluginAction<EditRasterInfo>(
            PluginType.FFmpeg,
            "EditRaster",
            createmedia<EditRasterInfo>,
            start
        );


        ValueTask<T> createmedia<T>() where T : MediaEditInfo, new() => new T().AsVTask();
        async ValueTask<string[]> start<T>(string[] files, IncomingTask task, T data) where T : MediaEditInfo =>
            await Task.WhenAll(files.Select(x => exec(x, data))).ConfigureAwait(false);

        async Task<string> exec<T>(string file, T data) where T : MediaEditInfo
        {
            var tempfile = Path.GetTempFileName();

            // force rewrite output file if exists
            var args = "-y ";

            // input file; TODO: download file before
            args += "-i {pathabvobaobaodjfd!!!!} ";

            args += data.ConstructFFMpegArguments() + " ";

            // don't reencode audio
            args += $"-c:a copy ";

            // output format
            args += $"-f {Path.GetExtension(tempfile)} ";

            // output path
            args += $" {tempfile} ";


            // TODO: get path
            var exepath = File.Exists("/bin/ffmpeg") ? "/bin/ffmpeg" : "assets/ffmpeg.exe";

            var process = Process.Start(new ProcessStartInfo(exepath, args));
            if (process is null) throw new InvalidOperationException("Could not start plugin process");

            await process.WaitForExitAsync().ConfigureAwait(false);
            return tempfile;
        }
    }
    static async ValueTask<FFProbeInfo?> FFProbe(string[] files)
    {
        var ffprobe =
            File.Exists("/bin/ffprobe") ? "/bin/ffprobe"
            : File.Exists("assets/ffprobe.exe") ? "assets/ffprobe.exe"
            : null;

        if (ffprobe is null) return null;

        var proc = Process.Start(new ProcessStartInfo(ffprobe, $"-v quiet -print_format json -show_streams \"{files[0]}\"") { RedirectStandardOutput = true });
        if (proc is null) return null;

        var str = await proc.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        return JsonConvert.DeserializeObject<FFProbeInfo>(str);
    }


    record FFProbeInfo(ImmutableArray<FFProbeStreamInfo> Streams);
    record FFProbeStreamInfo(string CodecName, int Width, int Height);
}