using System.Net;

namespace Transport.Upload._3DModelsUpload;

internal interface I3DModelUploader
{
    Task UploadAsync(
        Composite3DModel composite3DModel,
        NetworkCredential credential,
        CancellationToken cancellationToken);
}
