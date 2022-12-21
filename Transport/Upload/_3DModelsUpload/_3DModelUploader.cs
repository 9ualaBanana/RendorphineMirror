using System.Net;
using Transport.Upload._3DModelsUpload._3DModelDS;
using Transport.Upload._3DModelsUpload.CGTrader._3DModelComponents;
using Transport.Upload._3DModelsUpload.CGTrader.Upload;
using Transport.Upload._3DModelsUpload.Turbosquid.Upload;

namespace Transport.Upload._3DModelsUpload;

public static class _3DModelUploader
{
    public static async Task UploadAsync(
        Composite3DModel composite3DModel,
        NetworkCredential credential,
        CancellationToken cancellationToken = default)
    {
        await new TurboSquid3DModelUploader().UploadAsync(composite3DModel, credential, cancellationToken);
        await (composite3DModel.Metadata switch
        {
            CGTrader3DModelMetadata =>
                new CGTrader3DModelUploader().UploadAsync(composite3DModel, credential, cancellationToken),
            //TurboSquid3DModelMetadata =>
            //    new TurboSquid3DModelUploader(httpClient).UploadAsync(credential, composite3DModel, cancellationToken),
            { } unsupportedType => throw new ArgumentOutOfRangeException(
                nameof(unsupportedType), unsupportedType.GetType(), "Unsupported metadata type."
                )
        });
    }
}
