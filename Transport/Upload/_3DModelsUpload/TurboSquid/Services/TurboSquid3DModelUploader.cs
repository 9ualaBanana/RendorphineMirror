using Transport.Upload._3DModelsUpload.Models;
using Transport.Upload._3DModelsUpload.Services;
using Transport.Upload._3DModelsUpload.TurboSquid.Models;

namespace Transport.Upload._3DModelsUpload.TurboSquid.Services;

internal class TurboSquid3DModelUploader : I3DModelUploader<TurboSquid3DModelMetadata>
{
    public Task UploadAsync(Composite3DModel composite3DModel, TurboSquid3DModelMetadata metadata)
    {
        throw new NotImplementedException();
    }
}
