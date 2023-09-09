using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Upload;
using System.Net;

namespace _3DProductsPublish;

public static class _3DProductPublisher
{
    public static async Task PublishAsync(
        this _3DProduct _3DProduct,
        _3DProduct.Metadata_ metadata,
        NetworkCredential credential,
        CancellationToken cancellationToken = default)
    {
        await
            (await TurboSquid3DProductPublisher.InitializeAsync(credential, cancellationToken))
            .PublishAsync(await _3DProduct.AsyncWithTurboSquid(metadata, cancellationToken), credential, cancellationToken);
        // It should be published to each stock and not switched on metadata as it should be generic to be used for each stock and obtain some specific metadata in further processing steps.
        //await (_3DProduct.Metadata switch
        //{
        //    CGTrader3DProductMetadata =>
        //        new CGTrader3DProductPublisher().PublishAsync(_3DProduct, credential, cancellationToken),
        //    TurboSquid3DProductMetadata =>
        //        new TurboSquid3DProductPublisher().PublishAsync(_3DProduct, credential, cancellationToken),
        //    { } unsupportedType => throw new ArgumentOutOfRangeException(
        //        nameof(unsupportedType), unsupportedType.GetType(), "Unsupported metadata type."
        //        )
        //});
    }
}
