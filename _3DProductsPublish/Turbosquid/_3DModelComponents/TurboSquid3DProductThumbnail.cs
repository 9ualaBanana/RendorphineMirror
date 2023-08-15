using _3DProductsPublish._3DModelDS;

namespace _3DProductsPublish.Turbosquid._3DModelComponents;

internal record TurboSquid3DProductThumbnail : _3DProductThumbnail
{
    internal ThumbnailType Type => Path.GetFileNameWithoutExtension(FilePath).StartsWith("wire") ?
        ThumbnailType.wireframe : ThumbnailType.regular;

    internal TurboSquid3DProductThumbnail(string path) : base(path)
    {
    }
}

enum ThumbnailType { regular, wireframe }
