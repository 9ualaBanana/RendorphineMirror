namespace Node.Tasks.Exec.FFmpeg.Codecs;

public class H264NvencFFmpegCodec : IFFmpegCodec
{
    public required BitrateData Bitrate { get; init; }

    public IFFmpegCodec Fallback => new LibX264FFmpegCodec() { Bitrate = Bitrate };

    public IEnumerable<string> BuildInputArgs() => new ArgList()
    {
        // cuvid decoder doesn't like >32 decode surfaces which depend on the number of threads
        // but the correlation seems semi-random so we just use 1
        "-threads", "1",
    };

    public IEnumerable<string> BuildOutputArgs()
    {
        return new ArgList()
        {
            "-c:v", "h264_nvenc",
            "-preset:v", "p7",
            "-tune:v", "hq",

            Bitrate switch
            {
                BitrateData.Constant c => new[]
                {
                    "-rc:v", "cbr", // use constant bitrate
                    "-b:v", c.Bitrate, // set bitrate
                },
                BitrateData.Variable v => new[]
                {
                    "-cq:v", v.Quality,
                },
                _ => throw new Exception("Invalid bitrate"),
            },
        };
    }
}