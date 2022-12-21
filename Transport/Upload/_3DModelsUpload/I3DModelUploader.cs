using System.Net;
using Transport.Upload._3DModelsUpload._3DModelDS;

namespace Transport.Upload._3DModelsUpload;

internal interface I3DModelUploader
{
    Task UploadAsync(
        Composite3DModel composite3DModel,
        NetworkCredential credential,
        CancellationToken cancellationToken);
}
