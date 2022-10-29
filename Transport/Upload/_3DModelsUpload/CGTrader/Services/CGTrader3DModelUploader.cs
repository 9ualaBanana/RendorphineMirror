using Transport.Upload._3DModelsUpload.CGTrader.Models;
using Transport.Upload._3DModelsUpload.Models;
using Transport.Upload._3DModelsUpload.Services;

namespace Transport.Upload._3DModelsUpload.CGTrader.Services;

internal class CGTrader3DModelUploader : I3DModelUploader<CGTrader3DModelMetadata>
{
    public Task UploadAsync(Composite3DModel composite3DModel, CGTrader3DModelMetadata metadata)
    {
        throw new NotImplementedException();
    }
}
