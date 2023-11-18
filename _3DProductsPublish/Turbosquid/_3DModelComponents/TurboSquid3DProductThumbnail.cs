using _3DProductsPublish._3DProductDS;

namespace _3DProductsPublish.Turbosquid._3DModelComponents;

internal static class TurboSquid3DProductThumbnail
{
    internal static Type TurboSquidType(this _3DProductThumbnail thumbnail)
        => Path.GetFileNameWithoutExtension(thumbnail.FilePath).StartsWith("wire") ?
        Type.wireframe : Type.regular;

    internal enum Type { regular, wireframe }
}
