using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;

namespace _3DProductsPublish.Turbosquid.Upload.Processing;

// Store Processed equivalents of assets inside _3DProduct. Those processed assets will represent assets synced with turbosquid
// and will be children of base asset classes like _3DProductThumbnail, _3DModel, _3DProduct.Texture_.
// Convert to ABC which is generic on <TAsset> where TAsset : I3DProductAsset
internal interface ITurboSquidProcessed3DProductAsset : I3DProductAsset { string FileId { get; } }

static class TurboSquidProcessed3DProductAssetFactory
{
    public static ITurboSquidProcessed3DProductAsset Create(I3DProductAsset asset, string fileId)
        => asset switch
        {
            _3DModel<TurboSquid3DModelMetadata> _3DModel => new TurboSquidProcessed3DModel(_3DModel, fileId),
            _3DProductThumbnail thumbnail => new TurboSquidProcessed3DProductThumbnail(thumbnail, fileId),
            _3DProduct.Texture_ texture => new TurboSquidProcessed3DProductTexture(texture, fileId),
            _ => throw new ArgumentException("Unsupported asset type.")
        };
}

internal record TurboSquidProcessed3DModel
    : _3DModel<TurboSquid3DModelMetadata>, ITurboSquidProcessed3DProductAsset
{
    public string FileId { get; }

    internal TurboSquidProcessed3DModel(_3DModel<TurboSquid3DModelMetadata> _3DModel, string fileId)
        : base(_3DModel)
    {
        FileId = fileId;
    }
}

internal class TurboSquidProcessed3DProductThumbnail
    : _3DProductThumbnail, ITurboSquidProcessed3DProductAsset
{
    public string FileId { get; }

    internal TurboSquidProcessed3DProductThumbnail(_3DProductThumbnail thumbnail, string fileId)
        : base(thumbnail)
    {
        FileId = fileId;
    }

    // Type posted in the final product form request.
    internal enum Type_ { image, wireframe }
    internal Type_ Type => PreprocessedType(this) switch
    {
        PreprocessedType_.regular => Type_.image,
        PreprocessedType_.wireframe => Type_.wireframe,
        _ => throw new NotImplementedException()
    };

    internal enum PreprocessedType_ { regular, wireframe }
    internal static PreprocessedType_ PreprocessedType(_3DProductThumbnail thumbnail)
        => Path.GetFileNameWithoutExtension(thumbnail.FilePath).StartsWith("wire") ?
        PreprocessedType_.wireframe : PreprocessedType_.regular;
}

internal record TurboSquidProcessed3DProductTexture
    : _3DProduct.Texture_, ITurboSquidProcessed3DProductAsset
{
    public string FileId { get; }

    internal TurboSquidProcessed3DProductTexture(_3DProduct.Texture_ texture, string fileId)
        : base(texture)
    {
        FileId = fileId;
    }
}
