using System.Net;
using Transport.Upload._3DModelsUpload.CGTrader._3DModelComponents;
using Transport.Upload._3DModelsUpload.CGTrader.Upload;
using Transport.Upload._3DModelsUpload.Turbosquid.Upload;

namespace Transport.Upload._3DModelsUpload;

public static class _3DModelUploader
{
    public static async Task UploadAsync(
        this HttpClient httpClient,
        NetworkCredential credential,
        Composite3DModel composite3DModel,
        CancellationToken cancellationToken = default)
    {
        composite3DModel.Archive();
        await (composite3DModel.Metadata switch
        {
            CGTrader3DModelMetadata =>
                new CGTrader3DModelUploader(httpClient).UploadAsync(credential, composite3DModel, cancellationToken),
            { } unsupportedType => throw new ArgumentOutOfRangeException(
                nameof(unsupportedType), unsupportedType.GetType(), "Unsupported metadata type."
                )
        });
    }
}
