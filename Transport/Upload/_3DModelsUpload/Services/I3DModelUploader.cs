using Transport.Upload._3DModelsUpload.Models;

namespace Transport.Upload._3DModelsUpload.Services;

internal interface I3DModelUploader<TMetadata> where TMetadata : _3DModelMetadata
{
    Task UploadAsync(Composite3DModel composite3DModel, TMetadata metadata);
}
