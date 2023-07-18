namespace Node.Tasks.Exec.FFmpeg.Codecs;

public class LibX264FFmpegCodec : IFFmpegCodec
{
    public required BitrateData Bitrate { get; init; }

    public IEnumerable<string> BuildOutputArgs()
    {
        return new ArgList()
        {
            "-c:v", "h264",
            "-preset:v", "slow",
            "-tune:v", "film",

            Bitrate switch
            {
                BitrateData.Constant c => new[] { "-b:v", c.Bitrate, },
                BitrateData.Variable v => new[] { "-crf", v.Quality, },
                _ => throw new Exception("Invalid bitrate"),
            },
        };
    }
}