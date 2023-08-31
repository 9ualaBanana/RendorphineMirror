using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Upload;
using _3DProductsPublish.Turbosquid.Upload.Processing;
using _3DProductsPublish.Turbosquid.Upload.Requests;

namespace _3DProductsPublish.Turbosquid.Api;

internal class TurboSquidUploadApi
{
    readonly HttpClient _httpClient;
    readonly TurboSquid3DProductUploadSessionContext _uploadSessionContext;

    internal TurboSquidUploadApi(HttpClient httpClient, TurboSquid3DProductUploadSessionContext uploadSessionContext)
    {
        _httpClient = httpClient;
        _uploadSessionContext = uploadSessionContext;
    }

    internal async Task<TurboSquidUploaded3DProductAssets>UploadAssetsAsync(CancellationToken cancellationToken)
    {
        var uploadedModels = await UploadModelsAsync().ToListAsync(cancellationToken);
        var uploadedThumbnails = await UploadThumbnailsAsync().ToListAsync(cancellationToken);
        return new(uploadedModels, uploadedThumbnails);


        async IAsyncEnumerable<ITurboSquidProcessed3DProductAsset<_3DModel<TurboSquid3DModelMetadata>>> UploadModelsAsync()
        {
            var modelsUpload = await _uploadSessionContext.ProductDraft._Product._3DModels
                .Select(async _3DModel =>
                {
                    var archived3DModelPath = await _3DModel.ArchiveAsync(cancellationToken);
                    string uploadKey = await UploadAssetAsyncAt(archived3DModelPath, cancellationToken);
                    return await TurboSquid3DProductAssetProcessing.Task_<_3DModel<TurboSquid3DModelMetadata>>
                        .RunAsync(_3DModel, uploadKey, _uploadSessionContext, _httpClient, cancellationToken);
                })
                .RunAsync();
            foreach (var processedAsset in await TurboSquid3DProductAssetProcessing.Task_<_3DModel<TurboSquid3DModelMetadata>>.WhenAll(modelsUpload))
                yield return processedAsset;
        }

        async IAsyncEnumerable<ITurboSquidProcessed3DProductAsset<TurboSquid3DProductThumbnail>> UploadThumbnailsAsync()
        {
            var thumbnailsUpload = await _uploadSessionContext.ProductDraft.UpcastThumbnailsTo<TurboSquid3DProductThumbnail>()
                .Select(async thumbnail =>
                {
                    string uploadKey = await UploadAssetAsyncAt(thumbnail.FilePath, cancellationToken);
                    return await TurboSquid3DProductAssetProcessing.Task_<TurboSquid3DProductThumbnail>
                        .RunAsync(thumbnail, uploadKey, _uploadSessionContext, _httpClient, cancellationToken);
                })
                .RunAsync();
            foreach (var processedAsset in await TurboSquid3DProductAssetProcessing.Task_<TurboSquid3DProductThumbnail>.WhenAll(thumbnailsUpload))
                yield return processedAsset;
        }
    }

    /// <inheritdoc cref="AssetUploadRequest.SendAsyncUsing(HttpClient, CancellationToken)"/>
    async Task<string> UploadAssetAsyncAt(string assetPath, CancellationToken cancellationToken)
    {
        using var asset = File.OpenRead(assetPath);
        int partsCount = (int)Math.Ceiling(asset.Length / (double)MultipartAssetUploadRequest.MaxPartSize);
        return partsCount == 1 ? await UploadAssetAsSinglepartAsync() : await UploadAssetAsMultipartAsync();


        async Task<string> UploadAssetAsSinglepartAsync() => await
            (await SinglepartAssetUploadRequest.CreateAsyncFor(asset, _uploadSessionContext))
            .SendAsyncUsing(_httpClient, cancellationToken);

        async Task<string> UploadAssetAsMultipartAsync() => await
            (await MultipartAssetUploadRequest.CreateAsyncFor(asset, _uploadSessionContext, partsCount))
            .SendAsyncUsing(_httpClient, cancellationToken);
    }
}
