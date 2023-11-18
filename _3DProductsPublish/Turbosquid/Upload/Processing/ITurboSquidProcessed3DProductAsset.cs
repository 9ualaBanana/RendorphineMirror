using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;

namespace _3DProductsPublish.Turbosquid.Upload.Processing;

internal interface ITurboSquidProcessed3DProductAsset<TAsset>
    where TAsset : I3DProductAsset
{
    string FileId { get; }
    TAsset Asset { get; }
}

static class TurboSquidProcessed3DProductAssetFactory
{
    public static ITurboSquidProcessed3DProductAsset<TAsset> Create<TAsset>(TAsset asset, string fileId)
        where TAsset : I3DProductAsset
        => asset switch
        {
            _3DModel<TurboSquid3DModelMetadata> _3DModel => (new TurboSquidProcessed3DModel(_3DModel, fileId) as ITurboSquidProcessed3DProductAsset<TAsset>)!,
            _3DProductThumbnail thumbnail => (new TurboSquidProcessed3DProductThumbnail(thumbnail, fileId) as ITurboSquidProcessed3DProductAsset<TAsset>)!,
            _ => throw new ArgumentException("Unsupported asset type.")
        };
}

internal class TurboSquidProcessed3DModel
    : _3DModel<TurboSquid3DModelMetadata>, ITurboSquidProcessed3DProductAsset<_3DModel<TurboSquid3DModelMetadata>>
{
    public string FileId { get; }
    public _3DModel<TurboSquid3DModelMetadata> Asset => this;

    internal TurboSquidProcessed3DModel(_3DModel<TurboSquid3DModelMetadata> _3DModel, string fileId)
        : base(_3DModel)
    {
        FileId = fileId;
    }
}

internal record TurboSquidProcessed3DProductThumbnail
    : _3DProductThumbnail, ITurboSquidProcessed3DProductAsset<_3DProductThumbnail>
{
    public string FileId { get; }
    public _3DProductThumbnail Asset => this;

    internal TurboSquidProcessed3DProductThumbnail(_3DProductThumbnail thumbnail, string fileId)
        : base(thumbnail)
    {
        FileId = fileId;
    }


    internal enum Type { image, wireframe }
}

static class TurboSquidProcessed3DProductThumbnailExtensions
{
    internal static TurboSquidProcessed3DProductThumbnail.Type Type(this ITurboSquidProcessed3DProductAsset<_3DProductThumbnail> processedThumbnail)
        => processedThumbnail.Asset.TurboSquidType() switch
    {
        TurboSquid3DProductThumbnail.Type.regular => TurboSquidProcessed3DProductThumbnail.Type.image,
        TurboSquid3DProductThumbnail.Type.wireframe => TurboSquidProcessed3DProductThumbnail.Type.wireframe,
        _ => throw new NotImplementedException()
    };
}
