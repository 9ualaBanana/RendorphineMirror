using System.Diagnostics;
using Newtonsoft.Json;

namespace Node.Tasks.Exec;

public class Crop
{
    public int X, Y, W, H;
}
public abstract class MediaEditInfo : IPluginActionData
{
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
    public double? CutFrameAt;

    public override string ConstructFFMpegArguments()
    {
        var args = base.ConstructFFMpegArguments();

        // TODO:

        return args;
    }
}
public class EditRasterInfo : MediaEditInfo { }

public static class FFMpegTasks
{
    public static IEnumerable<IPluginAction> Create()
    {
        yield return new PluginAction<EditVideoInfo>(PluginType.FFmpeg, "EditVideo", Start);
        yield return new PluginAction<EditRasterInfo>(PluginType.FFmpeg, "EditRaster", Start);
    }
    static async ValueTask<string[]> Start<T>(string[] files, ReceivedTask task, T data) where T : MediaEditInfo =>
        await Task.WhenAll(files.Select(x => Execute(x, task, data))).ConfigureAwait(false);

    static async Task<string> Execute<T>(string input, ReceivedTask task, T data) where T : MediaEditInfo
    {
        var output = Path.Combine(Init.TaskFilesDirectory, task.Id, Path.GetFileNameWithoutExtension(input) + "_out" + Path.GetExtension(input));
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);

        // force rewrite output file if exists
        var args = "-y ";

        // input file
        args += $"-i {input} ";

        args += data.ConstructFFMpegArguments() + " ";

        // don't reencode audio
        args += $"-c:a copy ";

        // output format
        args += $"-f {Path.GetExtension(output).Replace(".", "")} ";

        // output path
        args += $" {output} ";


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

        var proc = Process.Start(new ProcessStartInfo(ffprobe, $"-v quiet -print_format json -show_streams \"{files[0]}\"") { RedirectStandardOutput = true });
        if (proc is null) return null;

        var str = await proc.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        return JsonConvert.DeserializeObject<FFProbeInfo>(str);
    }


    record FFProbeInfo(ImmutableArray<FFProbeStreamInfo> Streams);
    record FFProbeStreamInfo(string CodecName, int Width, int Height);
}