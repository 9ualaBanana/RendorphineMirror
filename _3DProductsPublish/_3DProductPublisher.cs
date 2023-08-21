using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.CGTrader._3DModelComponents;
using _3DProductsPublish.CGTrader.Upload;
using _3DProductsPublish.Turbosquid.Upload;
using System.Net;

namespace _3DProductsPublish;

public static class _3DProductPublisher
{
    public static async Task PublishAsync(
        this _3DProduct _3DProduct,
        NetworkCredential credential,
        CancellationToken cancellationToken = default)
    {
        await new TurboSquid3DProductPublisher().PublishAsync(_3DProduct, credential, cancellationToken);
        await (_3DProduct.Metadata switch
        {
            CGTrader3DProductMetadata =>
                new CGTrader3DProductPublisher().PublishAsync(_3DProduct, credential, cancellationToken),
            //TurboSquid3DModelMetadata =>
            //    new TurboSquid3DModelUploader(httpClient).UploadAsync(credential, composite3DModel, cancellationToken),
            { } unsupportedType => throw new ArgumentOutOfRangeException(
                nameof(unsupportedType), unsupportedType.GetType(), "Unsupported metadata type."
                )
        });
    }
}
