namespace Node.Tasks.Exec.Actions;

public static class FFMpegExec
{
    static async Task<string> JustExecuteFFMpeg(ITaskExecutionContext context, FileWithFormat file, Func<FFMpegArgsHolder, string> argfunc)
    {
        var inputfile = file.Path;
        var ffprobe = await FFProbe.Get(inputfile, context) ?? throw new Exception();
        var argholder = new FFMpegArgsHolder(file.Format, ffprobe);
        var outputfilename = argfunc(argholder);

        var args = FFMpegExec.GetFFMpegArgs(inputfile, outputfilename, context, argholder);

        var duration = TimeSpan.FromSeconds(ffprobe.Format.Duration);
        context.LogInfo($"{inputfile} duration: {duration} x{argholder.Rate}");
        duration /= argholder.Rate;

        await new NodeProcess(context.GetPlugin(PluginType.FFmpeg).Path, args, context, onRead, stderr: LogLevel.Trace).Execute();
        return outputfilename;



        void onRead(bool err, string line)
        {
            // frame=  502 fps=0.0 q=29.0 size=     256kB time=00:00:14.84 bitrate= 141.3kbits/s speed=29.5x
            if (!line.StartsWith("frame=", StringComparison.Ordinal)) return;

            var spt = line.AsSpan(line.IndexOf("time=", StringComparison.Ordinal) + "time=".Length).TrimStart();
            spt = spt.Slice(0, spt.IndexOf(' '));
            if (!TimeSpan.TryParse(spt, out var time))
                time = TimeSpan.Zero;

            context.SetProgress(Math.Clamp(time / duration, 0, 1));
        }
    }
    public static Task ExecuteFFMpeg(ITaskExecutionContext context, FileWithFormat file, Func<FFMpegArgsHolder, string> argfunc) => JustExecuteFFMpeg(context, file, argfunc);
    public static async Task ExecuteFFMpeg(ITaskExecutionContext context, FileWithFormat file, TaskFileListList outfiles, Func<FFMpegArgsHolder, string> argfunc)
    {
        var outputfilename = await JustExecuteFFMpeg(context, file, argfunc);
        outfiles.AddFromLocalPath(Path.GetDirectoryName(outputfilename).ThrowIfNull());
    }

    public static IEnumerable<string> GetFFMpegArgs(string inputfile, string outputfile, ITaskExecutionContext context, FFMpegArgsHolder argholder)
    {
        var argsarr = argholder.Args;
        var audiofilters = argholder.AudioFilers;
        var filtergraph = argholder.Filtergraph;
        var nvidia = context.TryGetPlugin(PluginType.NvidiaDriver) is not null;
        var video = argholder.OutputFileFormat == FileFormat.Mov;

        return new ArgList()
        {
            // hide useless info
            "-hide_banner",

            // enable hardware acceleration if nvidia driver installed
            nvidia ? new[] { "-hwaccel", "auto", "-threads", "1" } : null,

            // force rewrite output file if exists
            "-y",
            // input file
            "-i", inputfile,

            // arguments
            argsarr,

            (video && nvidia) ? new[] { "-c:v", "h264_nvenc" } : null,

            // video filters
            filtergraph.Count == 0 ? null : new[] { "-filter_complex", string.Join(',', filtergraph) },

            // audio filters
            audiofilters.Count == 0 ? null : new[] { "-af", string.Join(',', audiofilters) },

            // output path
            outputfile,
        };
    }
}
