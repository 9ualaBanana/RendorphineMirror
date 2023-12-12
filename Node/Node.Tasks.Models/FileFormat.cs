using HeyRed.Mime;

namespace Node.Tasks.Models;

[JsonConverter(typeof(StringEnumConverter))]
public enum FileFormat
{
    Jpeg,
    Png,
    Mov,
    Eps,
    Mp3,
}

public static class FileFormatExtensions
{
    public static FileFormat FromFilename(string path) => FromMime(MimeTypesMap.GetMimeType(path));
    public static FileFormat FromMime(string mime)
    {
        if (mime.Contains("image", StringComparison.Ordinal))
        {
            if (mime.Contains("png", StringComparison.Ordinal))
                return FileFormat.Png;

            return FileFormat.Jpeg;
        }

        if (mime.Contains("video", StringComparison.Ordinal)) return FileFormat.Mov;
        if (mime.Contains("postscript", StringComparison.Ordinal)) return FileFormat.Eps;
        if (mime.Contains("vector", StringComparison.Ordinal)) return FileFormat.Eps;
        if (mime.Contains("audio", StringComparison.Ordinal)) return FileFormat.Mp3;

        throw new Exception($"Could not find {nameof(FileFormat)} for mime {mime}");
    }

    [return: NotNullIfNotNull(nameof(extension))]
    static string? AsExtension(string? extension)
    {
        extension = Path.GetExtension(extension);
        if (extension is null || extension.StartsWith('.'))
            return extension;

        return "." + extension;
    }
    public static string? AsExtension(this FileFormat format) => "." + format.ToString().ToLowerInvariant();
}
