using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using MarkTM.RFProduct;

namespace _3DProductsPublish.Turbosquid.Upload.Processing;

internal interface ITurboSquidProcessed3DProductAsset : I3DProductAsset;
internal interface ITurboSquidProcessed3DProductAsset<TAsset> : ITurboSquidProcessed3DProductAsset
    where TAsset : I3DProductAsset
{
    long FileId { get; }
    /// <summary>
    /// Reference to the original asset which is to be replaced by this processed asset in <see cref="_3DProduct"/>.
    /// </summary>
    TAsset Asset { get; }
}

static class TurboSquidProcessed3DProductAssetFactory
{
    public static ITurboSquidProcessed3DProductAsset<TAsset> Create<TAsset>(TAsset asset, long fileId)
        where TAsset : I3DProductAsset
        => asset switch
        {
            _3DModel<TurboSquid3DModelMetadata> _3DModel =>
                new TurboSquidProcessed3DModel(_3DModel, fileId) as ITurboSquidProcessed3DProductAsset<TAsset>,
            _3DProductThumbnail thumbnail =>
                new TurboSquidProcessed3DProductThumbnail(thumbnail, fileId) as ITurboSquidProcessed3DProductAsset<TAsset>,
            _3DProduct.Textures_ textures =>
                new TurboSquidProcessed3DProductTextures(textures, fileId) as ITurboSquidProcessed3DProductAsset<TAsset>,
            _ => throw new ArgumentException("Unsupported asset type.")
        } ?? throw new ArgumentNullException(nameof(asset));
}

public record TurboSquidProcessed3DModel : _3DModel<TurboSquid3DModelMetadata>,
    ITurboSquidProcessed3DProductAsset<_3DModel<TurboSquid3DModelMetadata>>
{
    public long FileId { get; }
    public _3DModel<TurboSquid3DModelMetadata> Asset { get; }

    internal TurboSquidProcessed3DModel(_3DModel<TurboSquid3DModelMetadata> _3DModel, long fileId)
        : base(_3DModel)
    {
        FileId = fileId;
        Asset = _3DModel;
        _3DModel.Metadata.ID = fileId;
    }
}

internal class TurboSquidProcessed3DProductThumbnail : _3DProductThumbnail,
    ITurboSquidProcessed3DProductAsset<_3DProductThumbnail>
{
    public long FileId { get; }
    public _3DProductThumbnail Asset { get; }

    internal TurboSquidProcessed3DProductThumbnail(_3DProductThumbnail thumbnail, long fileId)
        : base(thumbnail.Path)
    {
        FileId = fileId;
        Asset = thumbnail;
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
        => RFProduct._3D.Idea_.IsWireframe(thumbnail.Path) ?
        PreprocessedType_.wireframe : PreprocessedType_.regular;
}

internal record TurboSquidProcessed3DProductTextures : _3DProduct.Textures_,
    ITurboSquidProcessed3DProductAsset<_3DProduct.Textures_>
{
    public long FileId { get; }
    public _3DProduct.Textures_ Asset { get; }

    internal TurboSquidProcessed3DProductTextures(_3DProduct.Textures_ textures, long fileId)
        : base(textures)
    {
        FileId = fileId;
        Asset = textures;
    }
}
