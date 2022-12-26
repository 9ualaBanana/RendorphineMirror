using System.Net;
using Transport.Upload._3DModelsUpload._3DModelDS;
using Transport.Upload._3DModelsUpload.CGTrader._3DModelComponents;
using Transport.Upload._3DModelsUpload.CGTrader.Upload;
using Transport.Upload._3DModelsUpload.Turbosquid.Upload;

namespace Transport.Upload._3DModelsUpload;

public static class _3DProductUploader
{
    public static async Task UploadAsync(
        _3DProduct _3DProduct,
        NetworkCredential credential,
        CancellationToken cancellationToken = default)
    {
        await new TurboSquid3DProductUploader().UploadAsync(_3DProduct, credential, cancellationToken);
        await (_3DProduct.Metadata switch
        {
            CGTrader3DProductMetadata =>
                new CGTrader3DModelUploader().UploadAsync(_3DProduct, credential, cancellationToken),
            //TurboSquid3DModelMetadata =>
            //    new TurboSquid3DModelUploader(httpClient).UploadAsync(credential, composite3DModel, cancellationToken),
            { } unsupportedType => throw new ArgumentOutOfRangeException(
                nameof(unsupportedType), unsupportedType.GetType(), "Unsupported metadata type."
                )
        });
    }
}
