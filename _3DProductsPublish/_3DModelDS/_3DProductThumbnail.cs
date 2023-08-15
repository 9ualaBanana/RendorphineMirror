using Node.Common.Models;
using System.Net.Mime;

namespace _3DProductsPublish._3DModelDS;

public record _3DProductThumbnail : I3DProductAsset
{
    public readonly string FilePath;
    public readonly string FileName;
    internal ContentType MimeType;
    public long Size
    {
        get
        {
            if (_size is null)
            {
                using var fileStream = File.OpenRead(FilePath);
                _size = fileStream.Length;
            }

            return _size.Value;
        }
    }
    long? _size;
    public FileStream AsFileStream => File.OpenRead(FilePath);

    internal static IEnumerable<_3DProductThumbnail> EnumerateIn(string _3DModelDirectory) =>
        Directory.EnumerateFiles(_3DModelDirectory)
        .Where(HasValidExtension)
        .Select(thumbnailPath => new _3DProductThumbnail(thumbnailPath));

    static bool HasValidExtension(string pathOrExtension) =>
        _validExtensions.Contains(Path.GetExtension(pathOrExtension));

    readonly static string[] _validExtensions = { ".jpeg", ".jpg", ".png" };

    protected _3DProductThumbnail(string path)
    {
        FilePath = path;
        FileName = Path.GetFileName(path);
        MimeType = new(MimeTypes.GetMimeType(path));
    }
}
