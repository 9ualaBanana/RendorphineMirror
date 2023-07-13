namespace Node.Tasks.Exec.FFmpeg.Codecs;

public class JpegFFmpegCodec : IFFmpegCodec
{
    public IEnumerable<string> BuildOutputArgs()
    {
        return new ArgList()
        {
            // codec
            "-c:v", "mjpeg",

            // output format
            "-f", "image2",

            // something something sync frames
            "-vsync", "0",

            // high quality image
            "-qscale:v", "2",

            // replace resulting images; to remove a warning
            "-update", "true",

            // single frame, speeds up the processing
            "-frames:v", "1",
        };
    }
}