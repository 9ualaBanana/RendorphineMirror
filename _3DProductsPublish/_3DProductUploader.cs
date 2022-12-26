using _3DProductsPublish._3DModelDS;
using _3DProductsPublish.CGTrader._3DModelComponents;
using _3DProductsPublish.CGTrader.Upload;
using _3DProductsPublish.Turbosquid.Upload;
using System.Net;

namespace _3DProductsPublish;

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
