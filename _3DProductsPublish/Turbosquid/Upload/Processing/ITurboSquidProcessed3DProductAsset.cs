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
            TurboSquid3DProductThumbnail thumbnail => (new TurboSquidProcessed3DProductThumbnail(thumbnail, fileId) as ITurboSquidProcessed3DProductAsset<TAsset>)!,
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
    : TurboSquid3DProductThumbnail, ITurboSquidProcessed3DProductAsset<TurboSquid3DProductThumbnail>
{
    public string FileId { get; }
    public TurboSquid3DProductThumbnail Asset => this;

    internal TurboSquidProcessed3DProductThumbnail(TurboSquid3DProductThumbnail thumbnail, string fileId)
        : base(thumbnail)
    {
        FileId = fileId;
    }
}

static class TurboSquidProcessed3DProductThumbnailExtensions
{
    internal static ProcessedThumbnailType Type(this ITurboSquidProcessed3DProductAsset<TurboSquid3DProductThumbnail> processedThumbnail)
        => processedThumbnail.Asset.Type switch
    {
        ThumbnailType.regular => ProcessedThumbnailType.image,
        ThumbnailType.wireframe => ProcessedThumbnailType.wireframe,
        _ => throw new NotImplementedException()
    };
}

enum ProcessedThumbnailType { image, wireframe }
