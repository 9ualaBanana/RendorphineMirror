using _3DProductsPublish._3DModelDS;
using _3DProductsPublish.Turbosquid.Upload;

namespace _3DProductsPublish.Turbosquid._3DModelComponents;

public record TurboSquid3DProductMetadata : _3DModelMetadata
{
    internal List<TurboSquidUploaded3DProductThumbnail> UploadedThumbnails = new();
}
