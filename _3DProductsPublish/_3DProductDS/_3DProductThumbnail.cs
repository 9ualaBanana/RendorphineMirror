using MarkTM.RFProduct;
using System.Net.Mime;

namespace _3DProductsPublish._3DProductDS;

public class _3DProductThumbnail : I3DProductAsset, IEquatable<_3DProductThumbnail>
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

    internal static IEnumerable<_3DProductThumbnail> EnumerateAt(string _3DProductDirectory) =>
        Directory.EnumerateFiles(_3DProductDirectory)
        .Where(RFProduct._3D.Idea_.IsRender)
        .Select(_ => new _3DProductThumbnail(_));


    protected _3DProductThumbnail(_3DProductThumbnail original)
        : this(original.FilePath)
    {
    }
    protected _3DProductThumbnail(string path)
    {
        FilePath = path;
        FileName = Path.GetFileName(path);
        MimeType = new(MimeTypes.GetMimeType(path));
    }

    public override bool Equals(object? obj) => Equals(obj as _3DProductThumbnail);
    public bool Equals(_3DProductThumbnail? other)
        => FileName == other?.FileName && Size == other?.Size;
    public override int GetHashCode() => HashCode.Combine(FileName, Size);
}
