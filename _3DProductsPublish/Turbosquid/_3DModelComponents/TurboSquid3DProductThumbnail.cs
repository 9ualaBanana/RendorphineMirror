using _3DProductsPublish._3DModelDS;
using _3DProductsPublish.Turbosquid.Upload;
using System.Net.Http.Json;

namespace _3DProductsPublish.Turbosquid._3DModelComponents;

internal record TurboSquid3DProductThumbnail : _3DProductThumbnail
{
    internal ThumbnailType Type => Path.GetFileNameWithoutExtension(FilePath).StartsWith("wire") ?
        ThumbnailType.wireframe : ThumbnailType.regular;

    internal TurboSquid3DProductThumbnail(string path) : base(path)
    {
    }

    internal JsonContent ToProcessJsonContentUsing(TurboSquid3DProductUploadSessionContext uploadSessionContext, string uploadKey) =>
        JsonContent.Create(new
        {
            upload_key = uploadKey,
            resource = "thumbnails",
            attributes = new
            {
                draft_id = uploadSessionContext.ProductDraft._ID,
                name = FileName,
                size = Size,
                thumbnail_type = Type.ToString(),
                watermarked = false,
            },
            authenticity_token = uploadSessionContext.Credential._CsrfToken
        });
}

enum ThumbnailType { regular, wireframe }
