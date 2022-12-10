using System.Net;

namespace Transport.Upload._3DModelsUpload;

internal interface I3DModelUploader
{
    Task UploadAsync(
        NetworkCredential credential,
        Composite3DModel composite3DModel,
        CancellationToken cancellationToken);
}
