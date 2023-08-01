namespace Node.Common;

public static class FFProbe
{
    public static async Task<FFProbeInfo> Get(string file, ILogger logger)
    {
        using var _ = logger.BeginScope("FFprobe");

        // TODO:: get from plugin list
        var ffprobe = File.Exists("/bin/ffprobe") ? "/bin/ffprobe" : "assets/ffprobe.exe";

        var str = await new ProcessLauncher(ffprobe) { Logging = { ILogger = logger } }
            .WithArgs(args => args.Add(
                $"-hide_banner",
                $"-v", $"quiet",
                $"-print_format", $"json",
                $"-show_streams",
                $"-show_format",
                file
            ))
            .ExecuteFullAsync();

        return JsonConvert.DeserializeObject<FFProbeInfo>(str) ?? throw new Exception($"Could not parse ffprobe output: {str}");
    }
    public static async Task<FFProbeInfo> Get(string file, ILoggable? logobj)
    {
        // TODO:: get from plugin list
        var ffprobe = File.Exists("/bin/ffprobe") ? "/bin/ffprobe" : "assets/ffprobe.exe";

        var str = await new ProcessLauncher(ffprobe) { Logging = { Logger = NamedLogger.TryFrom("FFprobe", logobj) } }
            .WithArgs(args => args.Add(
                $"-hide_banner",
                $"-v", $"quiet",
                $"-print_format", $"json",
                $"-show_streams",
                $"-show_format",
                file
            ))
            .ExecuteFullAsync();

        return JsonConvert.DeserializeObject<FFProbeInfo>(str) ?? throw new Exception($"Could not parse ffprobe output: {str}");
    }


    public record FFProbeInfo(ImmutableArray<FFProbeStreamInfo> Streams, FFProbeFormatInfo Format)
    {
        // die if there are multiple video streams
        public FFProbeStreamInfo VideoStream
        {
            get
            {
                try { return Streams.Single(x => x.CodecType.Equals("video", StringComparison.OrdinalIgnoreCase) && x.FrameRate < 10000); }
                catch (Exception ex) { throw new Exception($"Found more than one video stream: {string.Join("; ", Streams)}", ex); }
            }
        }
    };

    public record FFProbeStreamInfo(
        int Width,
        int Height,
        [JsonProperty("codec_name")] string CodecName,
        [JsonProperty("codec_type")] string CodecType,
        [JsonProperty("r_frame_rate")] string FrameRateString,
        [JsonProperty("bit_rate")] string Bitrate,
        [JsonProperty("codec_tag_string")] string? CodecTagString = null
    )
    {
        public double FrameRate => double.Parse(FrameRateString.Split('/')[0]) / double.Parse(FrameRateString.Split('/')[1]);
    }

    public record FFProbeFormatInfo(double Duration);
}