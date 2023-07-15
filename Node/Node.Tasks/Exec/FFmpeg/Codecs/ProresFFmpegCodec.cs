namespace Node.Tasks.Exec.FFmpeg.Codecs;

public class ProresFFmpegCodec : IFFmpegCodec
{
    /// <summary> Quality, lower is better. Scale is 0-32, best range for space/quality is 9-13, default here is 4. </summary>
    public string QScale { get; init; } = "4";

    public required Profiles Profile { get; init; }

    public IEnumerable<string> BuildOutputArgs()
    {
        return new ArgList()
        {
            "-c:v", "prores_ks",
            "-qscale:v", QScale,

            "-pix_fmt", Profile <= Profiles.HQ ? "yuv422p10le" : "yuv444p10le",

            // profile directly affects bitrate
            "-profile:v", ((int) Profile).ToString(CultureInfo.InvariantCulture),
        };
    }

    public static Profiles CopyProfileFrom(FFProbe.FFProbeStreamInfo stream) =>
        stream.CodecTagString switch
        {
            "apco" or "ocpa" => Profiles.Proxy,
            "apcs" or "scpa" => Profiles.LT,
            "apcn" or "ncpa" => Profiles.SD,
            "apch" or "hcpa" => Profiles.HQ,
            "ap4h" or "h4pa" => Profiles.Best,
            _ => Profiles.HQ,
        };


    public enum Profiles
    {
        /// <summary> 422 Proxy </summary>
        Proxy = 0,

        /// <summary> 422 LT </summary>
        LT = 1,

        /// <summary> 422 Standard Definition </summary>
        SD = 2,

        /// <summary> 422 High Quality </summary>
        HQ = 3,

        /// <summary> 4444xq </summary>
        Best = 5,
    }
}