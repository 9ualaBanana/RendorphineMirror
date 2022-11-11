using Transport.Upload._3DModelsUpload.CGTrader.Models;
using Transport.Upload._3DModelsUpload.CGTrader.Services;
using Transport.Upload._3DModelsUpload.Models;
using Transport.Upload._3DModelsUpload.TurboSquid.Models;
using Transport.Upload._3DModelsUpload.TurboSquid.Services;

namespace Transport.Upload._3DModelsUpload.Services;

public static class _3DModelUploader
{
    public static async Task UploadAsync(
        this HttpClient httpClient,
        CGTraderNetworkCredential credential,
        Composite3DModel composite3DModel,
        _3DModelMetadata metadata,
        CancellationToken cancellationToken = default)
    {        
        composite3DModel.Archive();
        await (metadata switch
        {
            CGTrader3DModelMetadata cgTraderMetadata =>
                new CGTrader3DModelUploader(httpClient).UploadAsync(credential, composite3DModel, cgTraderMetadata, cancellationToken),
            TurboSquid3DModelMetadata turboSquidMetadata =>
                new TurboSquid3DModelUploader(httpClient).UploadAsync(credential, composite3DModel, turboSquidMetadata),
            { } unsupportedType => throw new ArgumentOutOfRangeException(
                nameof(unsupportedType),
                unsupportedType.GetType(),
                "Unsupported metadata type.")
        });
    }
}
