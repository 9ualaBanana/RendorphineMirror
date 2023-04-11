namespace Node.Tasks.Exec;

public static class FFMpegExec
{
    public static IEnumerable<string> GetFFMpegArgs(string inputfile, string outputfile, ReceivedTask task, FFMpegArgsHolder argholder)
    {
        var argsarr = argholder.Args;
        var audiofilters = argholder.AudioFilers;
        var filtergraph = argholder.Filtergraph;
        var nvidia = PluginsManager.GetInstalledPluginsCache()?.Any(p => p.Type == PluginType.NvidiaDriver) == true;
        var video = argholder.OutputFileFormat == FileFormat.Mov;

        return new ArgList()
        {
            // hide useless info
            "-hide_banner",

            // enable hardware acceleration if nvidia driver installed
            (nvidia ? new[] { "-hwaccel", "auto", "-threads", "1" } : null ),

            // force rewrite output file if exists
            "-y",
            // input file
            "-i", inputfile,

            // arguments
            argsarr,

            ((video && nvidia) ? new[] { "-c:v", "h264_nvenc" } : null),

            // video filters
            filtergraph.Count == 0 ? null : new[] { "-filter_complex", string.Join(',', filtergraph) },

            // audio filters
            audiofilters.Count == 0 ? null : new[] { "-af", string.Join(',', audiofilters) },

            // output path
            outputfile,
        };
    }
}
