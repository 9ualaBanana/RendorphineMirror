using Transport.Upload._3DModelsUpload.CGTrader;
using Transport.Upload._3DModelsUpload.TurboSquid.Models;

namespace Transport.Upload._3DModelsUpload.TurboSquid.Services;

internal class TurboSquid3DModelUploader : _3DModelUploaderBase<TurboSquid3DModelMetadata>
{
    internal TurboSquid3DModelUploader(HttpClient httpClient) : base(httpClient)
    {

    }

    internal override Task UploadAsync(
        CGTraderNetworkCredential credential,
        Composite3DModel composite3DModel,
        TurboSquid3DModelMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
