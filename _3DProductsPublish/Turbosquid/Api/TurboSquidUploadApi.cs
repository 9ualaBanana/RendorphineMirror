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
            await UploadModelAsync(_3DModel, cancellationToken);

        var processingThumbnails = new List<TurboSquidProcessing3DProductThumbnail>();
        foreach (var thumbnail in _uploadSessionContext.ProductDraft.UpcastThumbnailsTo<TurboSquid3DProductThumbnail>())
            processingThumbnails.Add(await UploadThumbnailAsync(thumbnail, cancellationToken));

        (_uploadSessionContext.ProductDraft._Product.Metadata as TurboSquid3DProductMetadata)!.UploadedThumbnails
            .AddRange(await RequestUploadedThumbnailsAsyncFor(processingThumbnails, cancellationToken));
    }

    async Task UploadModelAsync(_3DModel _3DModel, CancellationToken cancellationToken)
    {
        var archived3DModel = await _3DModel.ArchiveAsync(cancellationToken);
        string uploadKey = await UploadAssetAsync(archived3DModel, cancellationToken);
        await ProcessAssetAsync(_3DModel.ToProcessJsonContentUsing(_uploadSessionContext, uploadKey), cancellationToken);
    }

    async Task<TurboSquidProcessing3DProductThumbnail> UploadThumbnailAsync(TurboSquid3DProductThumbnail thumbnail, CancellationToken cancellationToken)
    {
        string uploadKey = await UploadAssetAsync(thumbnail.FilePath, cancellationToken);
        string processingId = await ProcessAssetAsync(thumbnail.ToProcessJsonContentUsing(_uploadSessionContext, uploadKey), cancellationToken);

        return new(thumbnail, processingId);
    }

    /// <returns>The processing ID.</returns>
    async Task<string> ProcessAssetAsync(HttpContent processHttpContent, CancellationToken cancellationToken)
    {
        var response = await
            (await _httpClient.PostAsync(
            new Uri(TurboSquidApi._BaseUri, "turbosquid/uploads//process"),
            processHttpContent,
            cancellationToken))
            .EnsureSuccessStatusCode()
            .Content.ReadAsStringAsync(cancellationToken);

        return (string)JObject.Parse(response)["id"]!;
    }

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

    async Task<List<TurboSquidUploaded3DProductThumbnail>> RequestUploadedThumbnailsAsyncFor(List<TurboSquidProcessing3DProductThumbnail> filesBeingProcessed, CancellationToken cancellationToken)
    {
        var uploadedFileIds = new List<TurboSquidUploaded3DProductThumbnail>(filesBeingProcessed.Count);
        while (filesBeingProcessed.Any())
        {
            foreach (var fileUploadStatus in await PollProcessingStatusAsyncForFilesWith(filesBeingProcessed.Select(file => file.ProcessingID), cancellationToken))
            {
                if ((string)fileUploadStatus["status"]! == "success")
                {
                    var processedThumbnail = filesBeingProcessed.Single(file => file.ProcessingID == (string)fileUploadStatus["id"]!);
                    uploadedFileIds.Add(new(processedThumbnail.Thumbnail, (int)fileUploadStatus["file_id"]!));
                    filesBeingProcessed.Remove(processedThumbnail);
                }
            }
        }
        return uploadedFileIds;
    }

    async Task<JEnumerable<JToken>> PollProcessingStatusAsyncForFilesWith(IEnumerable<string> ids, CancellationToken cancellationToken)
    {
        var response = await
            (await _httpClient.PostAsJsonAsync(new Uri(TurboSquidApi._BaseUri, "turbosquid/uploads/bulk_poll"), new
            {
                authenticity_token = _uploadSessionContext.Credential._CsrfToken,
                ids
            }, cancellationToken))
            .EnsureSuccessStatusCode()
            .Content.ReadAsStringAsync(cancellationToken);

        return JArray.Parse(response).Children();
    }
}

static class TurboSquid3DModelExtensions
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
