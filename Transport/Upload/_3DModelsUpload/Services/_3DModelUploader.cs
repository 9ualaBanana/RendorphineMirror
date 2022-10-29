using Transport.Upload._3DModelsUpload.CGTrader.Models;
using Transport.Upload._3DModelsUpload.CGTrader.Services;
using Transport.Upload._3DModelsUpload.Models;
using Transport.Upload._3DModelsUpload.TurboSquid.Models;
using Transport.Upload._3DModelsUpload.TurboSquid.Services;

namespace Transport.Upload._3DModelsUpload.Services;

public static class _3DModelUploader
{
    public static  async Task UploadAsync(_3DModel _3DModel, _3DModelMetadata metadata) =>
        await UploadAsync(new Composite3DModel(previews: null, _3DModel), metadata);

    public static async Task UploadAsync(Composite3DModel composite3DModel, _3DModelMetadata metadata)
    {
        composite3DModel.Archive();
        await (metadata switch
        {
            CGTrader3DModelMetadata cgTraderMetadata =>
                new CGTrader3DModelUploader().UploadAsync(composite3DModel, cgTraderMetadata),
            TurboSquid3DModelMetadata turboSquidMetadata =>
                new TurboSquid3DModelUploader().UploadAsync(composite3DModel, turboSquidMetadata),
            { } unsupportedType => throw new ArgumentOutOfRangeException(
                nameof(unsupportedType),
                unsupportedType.GetType(),
                "Unsupported metadata type.")
        });
    }
}
