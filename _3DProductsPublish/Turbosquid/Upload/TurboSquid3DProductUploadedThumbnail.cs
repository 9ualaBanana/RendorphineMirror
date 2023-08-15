using _3DProductsPublish.Turbosquid._3DModelComponents;

namespace _3DProductsPublish.Turbosquid.Upload;

internal record TurboSquid3DProductUploadedThumbnail : TurboSquid3DProductThumbnail
{
    internal readonly string FileId;

    new internal UploadedThumbnailType Type => base.Type switch
    {
        ThumbnailType.regular => UploadedThumbnailType.image,
        ThumbnailType.wireframe => UploadedThumbnailType.wireframe,
        _ => throw new NotImplementedException()
    };

    internal TurboSquid3DProductUploadedThumbnail(TurboSquid3DProductThumbnail thumbnail, string fileId)
        : base(thumbnail)
    {
        FileId = fileId;
    }
}

enum UploadedThumbnailType { image, wireframe }
