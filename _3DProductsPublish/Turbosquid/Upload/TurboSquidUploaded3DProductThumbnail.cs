using _3DProductsPublish.Turbosquid._3DModelComponents;

namespace _3DProductsPublish.Turbosquid.Upload;

internal record TurboSquidUploaded3DProductThumbnail
    (TurboSquid3DProductThumbnail Thumbnail, int UploadedFileID)
{
    internal UploadedThumbnailType Type => Thumbnail.Type switch
    {
        ThumbnailType.regular => UploadedThumbnailType.image,
        ThumbnailType.wireframe => UploadedThumbnailType.wireframe,
        _ => throw new NotImplementedException()
    };
}

enum UploadedThumbnailType { image, wireframe }
