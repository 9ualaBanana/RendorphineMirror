using _3DProductsPublish._3DModelDS;
using System.Net;

namespace _3DProductsPublish;

internal interface I3DProductUploader
{
    Task UploadAsync(
        _3DProduct _3DProduct,
        NetworkCredential credential,
        CancellationToken cancellationToken);
}
