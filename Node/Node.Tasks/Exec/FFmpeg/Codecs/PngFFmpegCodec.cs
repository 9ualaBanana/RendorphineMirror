namespace Node.Tasks.Exec.FFmpeg.Codecs;

public class PngFFmpegCodec : IFFmpegCodec
{
    public IEnumerable<string> BuildOutputArgs()
    {
        return new ArgList()
        {
            // codec
            "-c:v", "png",

            // output format
            "-f", "image2",

            // something something sync frames
            "-vsync", "0",

            // replace resulting images; to remove a warning
            "-update", "true",

            // single frame, speeds up the processing
            "-frames:v", "1",
        };
    }
}