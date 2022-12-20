using Transport.Models;
using Transport.Upload._3DModelsUpload.CGTrader._3DModelComponents;
using Transport.Upload._3DModelsUpload.CGTrader.Upload;
using Transport.Upload._3DModelsUpload.Turbosquid._3DModelComponents;
using Transport.Upload._3DModelsUpload.Turbosquid.Upload;
using Transport.Upload._3DModelsUpload.Turbosquid.Upload.Requests;

namespace Transport.Upload._3DModelsUpload.Turbosquid.Api;

internal class TurboSquidUploadApi : IBaseAddressProvider
{
    const int _MaxPartSize = 5242880;

    readonly HttpClient _httpClient;
    readonly TurboSquidAwsUploadCredentials _awsUploadCredentials;

    Uri _UploadEndpointWithoutQueryFor(string assetPath) => new((this as IBaseAddressProvider).Endpoint($"{Path.GetFileName(assetPath)}"));
    public string BaseAddress => _baseAddress;
    readonly string _baseAddress;

    internal TurboSquidUploadApi(HttpClient httpClient, TurboSquidAwsUploadCredentials awsUploadCredentials)
    {
        _httpClient = httpClient;
        _awsUploadCredentials = awsUploadCredentials;
        _baseAddress = $"https://{_awsUploadCredentials.Bucket}.s3.amazonaws.com/{_awsUploadCredentials.KeyPrefix}{DateTime.UtcNow.AsUnixTimestamp()}/";
    }

    internal async Task _UploadAssetsAsync(Composite3DModelDraft modelDraft, CancellationToken cancellationToken)
    {
        foreach (var _3DModel in modelDraft._Model._3DModels)
            foreach (var modelFilePath in _3DModel.Files)
                await _UploadAssetAsync(modelFilePath, modelDraft._DraftID, cancellationToken);

        foreach (var modelPreviewImage in modelDraft._UpcastPreviewImagesTo<TurboSquid3DModelPreviewImage>())
        {
            await _UploadAssetAsync(modelPreviewImage.FilePath, modelDraft._DraftID, cancellationToken);
            //(modelDraft._Model.Metadata as TurboSquid3DModelPreviewImage)!.UploadedPreviewImagesIDs.Add(uploadedFileId);
        }
    }

    async Task _UploadAssetAsync(string assetPath, string productDraftId, CancellationToken cancellationToken)
    {
        await _UploadAssetAsyncCore(assetPath, cancellationToken);
    }

    async Task _UploadAssetAsyncCore(string assetPath, CancellationToken cancellationToken)
    {
        using var asset = File.OpenRead(assetPath);
        int partsCount = (int)Math.Ceiling(asset.Length / (double)_MaxPartSize);

        if (partsCount == 1) await _UploadAssetAsSinglepartAsync(asset, cancellationToken);
        else await _UploadAssetAsMultipartAsync(asset, partsCount, cancellationToken);
    }

    async Task _UploadAssetAsSinglepartAsync(FileStream asset, CancellationToken cancellationToken) => await
        (await SinglepartAssetUploadRequest._CreateAsyncFor(asset, _UploadEndpointWithoutQueryFor(asset.Name), _awsUploadCredentials))
        ._SendAsyncUsing(_httpClient, cancellationToken);

    async Task _UploadAssetAsMultipartAsync(FileStream asset, int partsCount, CancellationToken cancellationToken) => await
        (await MultipartAssetUploadRequest._CreateAsyncFor(asset, _UploadEndpointWithoutQueryFor(asset.Name), _awsUploadCredentials))
        ._SendAsyncUsing(_httpClient, partsCount, cancellationToken);
}
