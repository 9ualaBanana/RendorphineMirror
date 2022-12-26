using System.Net.Http.Json;
using Transport.Upload._3DModelsUpload._3DModelDS;
using Transport.Upload._3DModelsUpload.Turbosquid.Upload;

namespace Transport.Upload._3DModelsUpload.Turbosquid._3DModelComponents;

internal record TurboSquid3DProductThumbnail : _3DProductThumbnail
{
    ThumbnailType _Type => Path.GetFileNameWithoutExtension(FilePath).StartsWith("wire") ?
        ThumbnailType.wireframe : ThumbnailType.regular;

    internal TurboSquid3DProductThumbnail(string path) : base(path)
    {
    }

    internal JsonContent _ToProcessJsonContentUsing(TurboSquid3DProductUploadSessionContext uploadSessionContext, string uploadKey) =>
        JsonContent.Create(new
        {
            upload_key = uploadKey,
            resource = "thumbnails",
            attributes = new
            {
                draft_id = uploadSessionContext.ProductDraft._ID,
                name = FileName,
                size = Size,
                thumbnail_type = _Type.ToString(),
                watermarked = false,
            },
            authenticity_token = uploadSessionContext.Credential._CsrfToken
        });
}

enum ThumbnailType { regular, wireframe }
