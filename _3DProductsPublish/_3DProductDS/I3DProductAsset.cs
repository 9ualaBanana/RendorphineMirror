using System.Net.Mime;

namespace _3DProductsPublish._3DProductDS;

public interface I3DProductAsset
{ string Path { get; init; } }

static class I3DProductAssetExtensions
{
    internal static string Name(this I3DProductAsset asset)
        => Path.GetFileName(asset.Path);
    internal static long Size(this I3DProductAsset asset)
    { using var file = File.OpenRead(asset.Path); return file.Length; }
    internal static ContentType MimeType(this I3DProductAsset asset)
        => new(MimeTypes.GetMimeType(asset.Path));
}
