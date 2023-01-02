namespace Common.Tasks;

public enum FileFormat { Jpeg, Mov, Eps }
public static class FileFormatExtensions
{
    public static FileFormat FromFilename(string path) => FromMime(MimeTypes.GetMimeType(path));
    public static FileFormat FromMime(string mime)
    {
        if (mime.Contains("image", StringComparison.Ordinal)) return FileFormat.Jpeg;
        if (mime.Contains("video", StringComparison.Ordinal)) return FileFormat.Mov;
        if (mime.Contains("postscript", StringComparison.Ordinal)) return FileFormat.Eps;
        if (mime.Contains("vector", StringComparison.Ordinal)) return FileFormat.Eps;

        throw new Exception($"Could not find {nameof(FileFormat)} for mime {mime}");
    }
}