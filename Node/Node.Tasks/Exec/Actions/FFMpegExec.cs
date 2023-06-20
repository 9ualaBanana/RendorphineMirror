namespace Node.Tasks.Exec.Actions;

public static class FFMpegExec
{
    static async Task<ReadOnlyTaskFileList> JustExecuteFFMpeg(ITaskExecutionContext context, FileWithFormat file, Func<FFMpegArgsHolder, string> argfunc)
    {
        var inputfile = file.Path;
        var ffprobe = await FFProbe.Get(inputfile, context) ?? throw new Exception();
        var argholder = new FFMpegArgsHolder(file.Format, ffprobe);
        var outputfilename = argfunc(argholder);

        try { return await start(FFMpegExec.GetFFMpegArgs(inputfile, outputfilename, context, argholder, true)); }
        catch (NodeProcessException ex)
        {
            try { File.Delete(outputfilename); }
            catch { }

            context.LogErr($"{ex.Message}, restarting without hwaccel");
            return await start(FFMpegExec.GetFFMpegArgs(inputfile, outputfilename, context, argholder, false));
        }


        async Task<ReadOnlyTaskFileList> start(IEnumerable<string> args)
        {
            var duration = TimeSpan.FromSeconds(ffprobe.Format.Duration);
            context.LogInfo($"{inputfile} duration: {duration} x{argholder.Rate}");
            duration /= argholder.Rate;

            var prevfiles = Directory.GetFiles(Path.GetDirectoryName(outputfilename)!);
            await new NodeProcess(context.GetPlugin(PluginType.FFmpeg).Path, args, context, onRead, stderr: LogLevel.Trace).Execute();
            var newfiles = Directory.GetFiles(Path.GetDirectoryName(outputfilename)!).Except(prevfiles);

            return new ReadOnlyTaskFileList(newfiles.Select(FileWithFormat.FromFile));


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
    }
    public static async Task ExecuteFFMpeg(ITaskExecutionContext context, FileWithFormat file, TaskFileList outfiles, Func<FFMpegArgsHolder, string> argfunc)
    {
        var result = await JustExecuteFFMpeg(context, file, argfunc);

        foreach (var res in result)
            outfiles.Add(res);
    }
    public static async Task ExecuteFFMpeg(ITaskExecutionContext context, FileWithFormat file, TaskFileListList outfiles, Func<FFMpegArgsHolder, string> argfunc)
    {
        var result = await JustExecuteFFMpeg(context, file, argfunc);

        foreach (var res in result)
            outfiles.New().Add(res);
    }

    public static IEnumerable<string> GetFFMpegArgs(string inputfile, string outputfile, ITaskExecutionContext context, FFMpegArgsHolder argholder, bool hardwareAcceleration)
    {
        var argsarr = argholder.Args;
        var audiofilters = argholder.AudioFilers;
        var filtergraph = argholder.Filtergraph;
        hardwareAcceleration &= context.TryGetPlugin(PluginType.NvidiaDriver) is not null;
        var video = argholder.OutputFileFormat == FileFormat.Mov;

        return new ArgList()
        {
            // hide useless info
            "-hide_banner",

            // enable hardware acceleration if nvidia driver installed
            hardwareAcceleration ? new[] { "-hwaccel", "auto", "-threads", "1" } : null,

            // force rewrite output file if exists
            "-y",
            // input file
            "-i", inputfile,

            // arguments
            argsarr,

            (video && hardwareAcceleration) ? new[] { "-c:v", "h264_nvenc" } : null,
            (video && hardwareAcceleration) ? new[] { "-preset:v", "p7", "-tune:v", "hq", "-rc:v", "vbr", "-cq:v", "19" } : null,

            (video && !hardwareAcceleration) ? new[] { "-c:v", "h264" } : null,
            (video && !hardwareAcceleration) ? new[] { "-crf", "18", "-preset:v", "slow", "-tune:v", "film" } : null,

            video ? new[] { "-b:v", "0" } : null,

            // video filters
            filtergraph.Count == 0 ? null : new[] { "-filter_complex", string.Join(',', filtergraph) },

            // audio filters
            audiofilters.Count == 0 ? null : new[] { "-af", string.Join(',', audiofilters) },

            // output path
            outputfile,
        };
    }
}
