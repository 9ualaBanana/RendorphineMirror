using System.Net;
using Transport.Upload._3DModelsUpload._3DModelDS;

namespace Transport.Upload._3DModelsUpload;

internal interface I3DProductUploader
{
    Task UploadAsync(
        _3DProduct _3DProduct,
        NetworkCredential credential,
        CancellationToken cancellationToken);
}
