using _3DProductsPublish._3DModelDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Upload;
using _3DProductsPublish.Turbosquid.Upload.Requests;

namespace _3DProductsPublish.Turbosquid.Api;

internal class TurboSquidUploadApi
{
    readonly TurboSquid3DProductAssetsProcessor _assetsProcessor;
    readonly HttpClient _httpClient;
    readonly TurboSquid3DProductUploadSessionContext _uploadSessionContext;

    internal TurboSquidUploadApi(HttpClient httpClient, TurboSquid3DProductUploadSessionContext uploadSessionContext)
    {
        _assetsProcessor = new(httpClient, uploadSessionContext);
        _httpClient = httpClient;
        _uploadSessionContext = uploadSessionContext;
    }

    internal async Task<TurboSquidUploaded3DProductAssets>
        UploadAssetsAsync(CancellationToken cancellationToken)
    {
        var uploadedModels = await UploadModelsAsync().ToListAsync(cancellationToken);
        var uploadedThumbnails = await UploadThumbnailsAsync().ToListAsync(cancellationToken);
        return new(uploadedModels, uploadedThumbnails);


        async IAsyncEnumerable<ITurboSquidProcessed3DProductAsset<_3DModel>> UploadModelsAsync()
        {
            var modelsUpload = _uploadSessionContext.ProductDraft._Product._3DModels
                .Select(async _3DModel =>
                {
                    var archived3DModelPath = await _3DModel.ArchiveAsync(cancellationToken);
                    string uploadKey = await UploadAssetAsyncAt(archived3DModelPath, cancellationToken);
                    return await _assetsProcessor.RunAsyncOn(_3DModel, uploadKey, cancellationToken);
                });
            await foreach (var processedAsset in _assetsProcessor.AwaitAsyncOn((await Task.WhenAll(modelsUpload)).ToList(), cancellationToken))
                yield return processedAsset;
        }

        async IAsyncEnumerable<ITurboSquidProcessed3DProductAsset<TurboSquid3DProductThumbnail>> UploadThumbnailsAsync()
        {
            var thumbnailsUpload = _uploadSessionContext.ProductDraft.UpcastThumbnailsTo<TurboSquid3DProductThumbnail>()
                .Select(async thumbnail =>
                {
                    string uploadKey = await UploadAssetAsyncAt(thumbnail.FilePath, cancellationToken);
                    return await _assetsProcessor.RunAsyncOn(thumbnail, uploadKey, cancellationToken);
                });
            await foreach (var processedAsset in _assetsProcessor.AwaitAsyncOn((await Task.WhenAll(thumbnailsUpload)).ToList(), cancellationToken))
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
