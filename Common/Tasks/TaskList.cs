using System.Diagnostics;
using Common.Tasks.Tasks.DTO;

namespace Common.Tasks;

public static class TaskList
{
    public static readonly ImmutableArray<PluginType> Types = Enum.GetValues<PluginType>().ToImmutableArray();
    public static ImmutableArray<IPluginAction> Actions;

    static TaskList()
    {
        var actions = new List<IPluginAction>();
        ffmpeg();

        Actions = actions.ToImmutableArray();


        void ffmpeg()
        {
            actions.Add(new PluginAction<EditVideoInfo>(
                PluginType.FFmpeg,
                "EditVideo",
                data => new EditVideoInfo().AsVTask(), // TODO: create default values based on `data`
                task => start(task)
            ));
            actions.Add(new PluginAction<EditRasterInfo>(
                PluginType.FFmpeg,
                "EditRaster",
                data => new EditRasterInfo().AsVTask(), // TODO: create default values based on `data`
                task => start(task)
            ));


            async ValueTask start<T>(NodeTask<T> task) where T : MediaEditInfo
            {
                var data = task.Data.MediaEditInfo;
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
            }
        }
    }

    public static IEnumerable<IPluginAction> Get(PluginType type) => Actions.Where(x => x.Type == type);
    public static IPluginAction? TryGet(string name) => Actions.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    public static IPluginAction? TryGet(PluginType type, string name) => Actions.FirstOrDefault(x => x.Type == type && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
