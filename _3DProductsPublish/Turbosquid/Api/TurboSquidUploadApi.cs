using _3DProductsPublish._3DModelDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Upload;
using _3DProductsPublish.Turbosquid.Upload.Requests;
using System.Net.Http.Json;

namespace _3DProductsPublish.Turbosquid.Api;

internal class TurboSquidUploadApi
{
    const int _MaxPartSize = 5242880;

    readonly HttpClient _httpClient;

    readonly TurboSquid3DProductUploadSessionContext _uploadSessionContext;

    internal TurboSquidUploadApi(HttpClient httpClient, TurboSquid3DProductUploadSessionContext uploadSessionContext)
    {
        _httpClient = httpClient;
        _uploadSessionContext = uploadSessionContext;
    }

    internal async Task UploadAssetsAsync(CancellationToken cancellationToken)
    {
        foreach (var _3DModel in _uploadSessionContext.ProductDraft._Product._3DModels)
            await _UploadModelAsync(_3DModel, cancellationToken);

        foreach (var thumbnail in _uploadSessionContext.ProductDraft.UpcastThumbnailsTo<TurboSquid3DProductThumbnail>())
        {
            await UploadThumbnailAsync(thumbnail, cancellationToken);
            //(modelDraft._Model.Metadata as TurboSquid3DModelPreviewImage)!.UploadedPreviewImagesIDs.Add(uploadedFileId);
        }
    }

    async Task _UploadModelAsync(_3DModel _3DModel, CancellationToken cancellationToken)
    {
        var archived3DModel = await _3DModel.ArchiveAsync(cancellationToken);
        string uploadKey = await UploadAssetAsync(archived3DModel, cancellationToken);
        await ProcessAssetAsync(_3DModel.ToProcessJsonContentUsing(_uploadSessionContext, uploadKey), cancellationToken);
    }

    // Should return `file_id` from https://www.squid.io/turbosquid/uploads/bulk_poll response or
    // return `id` from https://www.squid.io/turbosquid/uploads//process and then bulk poll `ids` for https://www.squid.io/turbosquid/products/0 request
    // (which is the one that uploads 3D product metadata) either from `UploadAssetsAsync`, or propagate response with ALL `ids` right to `TurboSquid3DProductUploader`
    // and bulk poll `file_ids` there and pass them to the method that uploads the metadata.
    async Task UploadThumbnailAsync(TurboSquid3DProductThumbnail thumbnail, CancellationToken cancellationToken)
    {
        string uploadKey = await UploadAssetAsync(thumbnail.FilePath, cancellationToken);
        await ProcessAssetAsync(thumbnail.ToProcessJsonContentUsing(_uploadSessionContext, uploadKey), cancellationToken);
    }

    async Task ProcessAssetAsync(HttpContent processHttpContent, CancellationToken cancellationToken) => 
        (await _httpClient.PostAsync(
            new Uri(TurboSquidApi._BaseUri, "turbosquid/uploads//process"),
            processHttpContent,
            cancellationToken))
            .EnsureSuccessStatusCode();

    async Task<string> UploadAssetAsync(string assetPath, CancellationToken cancellationToken)
    {
        using var asset = File.OpenRead(assetPath);
        int partsCount = (int)Math.Ceiling(asset.Length / (double)_MaxPartSize);

        return partsCount == 1 ?
            await UploadAssetAsSinglepartAsync(asset, cancellationToken) :
            await UploadAssetAsMultipartAsync(asset, partsCount, cancellationToken);
    }

    /// <inheritdoc cref="AssetUploadRequest.SendAsyncUsing(HttpClient, CancellationToken)"/>
    async Task<string> UploadAssetAsSinglepartAsync(FileStream asset, CancellationToken cancellationToken) => await
        (await SinglepartAssetUploadRequest.CreateAsyncFor(asset, _uploadSessionContext))
        .SendAsyncUsing(_httpClient, cancellationToken);

    /// <inheritdoc cref="AssetUploadRequest.SendAsyncUsing(HttpClient, CancellationToken)"/>
    async Task<string> UploadAssetAsMultipartAsync(FileStream asset, int partsCount, CancellationToken cancellationToken) => await
        (await MultipartAssetUploadRequest.CreateAsyncFor(asset, _uploadSessionContext, partsCount))
        .SendAsyncUsing(_httpClient, cancellationToken);
}

static class _TurboSquid3DModelExtensions
{
    internal static JsonContent ToProcessJsonContentUsing(this _3DModel _3DModel, TurboSquid3DProductUploadSessionContext uploadSessionContext, string uploadKey)
    {
        using var modelArchiveStream = File.OpenRead(_3DModel.ArchiveAsync().Result);
        return JsonContent.Create(new
        {
            upload_key = uploadKey,
            resource = "product_files",
            attributes = new
            {
                draft_id = uploadSessionContext.ProductDraft._ID,
                name = Path.GetFileName(modelArchiveStream.Name),
                size = modelArchiveStream.Length,
                format_version = string.Empty,
                renderer = string.Empty,
                renderer_version = string.Empty,
                is_native = false
            },
            authenticity_token = uploadSessionContext.Credential._CsrfToken
        });
    }
}
