using Common;
using System.Net.Mime;

namespace Transport.Upload._3DModelsUpload._3DModelDS;

public record _3DModelThumbnail
{
    public readonly string FilePath;
    public readonly string Name;
    internal ContentType MimeType;
    public FileStream AsFileStream => File.OpenRead(FilePath);

    internal static IEnumerable<_3DModelThumbnail> _EnumerateIn(string _3DModelDirectory) =>
        Directory.EnumerateFiles(_3DModelDirectory)
        .Where(_HasValidExtension)
        .Select(previewPath => new _3DModelThumbnail(previewPath));

    static bool _HasValidExtension(string pathOrExtension) =>
        _validExtensions.Contains(Path.GetExtension(pathOrExtension));

    readonly static string[] _validExtensions = { ".jpeg", ".jpg", ".png" };

    protected _3DModelThumbnail(string path)
    {
        FilePath = path;
        Name = Path.GetFileName(path);
        MimeType = new(MimeTypes.GetMimeType(path));
    }
}
