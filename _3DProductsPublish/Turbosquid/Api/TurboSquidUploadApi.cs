using _3DProductsPublish._3DModelDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Upload;
using _3DProductsPublish.Turbosquid.Upload.Requests;
using Newtonsoft.Json.Linq;
using System.Net.Http.Json;

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

    internal async Task UploadAssetsAsync(CancellationToken cancellationToken)
    {
        await UploadModelsAsync(cancellationToken);
        await UploadThumbnailsAsync(cancellationToken);
    }

    async Task UploadModelsAsync(CancellationToken cancellationToken)
    {
        foreach (var _3DModel in _uploadSessionContext.ProductDraft._Product._3DModels)
            await UploadModelAsync(_3DModel, cancellationToken);
    }

    async Task UploadModelAsync(_3DModel _3DModel, CancellationToken cancellationToken)
    {
        var archived3DModelPath = await _3DModel.ArchiveAsync(cancellationToken);
        string uploadKey = await UploadAssetAsyncAt(archived3DModelPath, cancellationToken);
        await ProcessAssetAsync(_3DModel.ToProcessJsonContentUsing(_uploadSessionContext, uploadKey), cancellationToken);
    }

    async Task UploadThumbnailsAsync(CancellationToken cancellationToken)
    {
        var thumbnailsUploadTasks = new List<TurboSquid3DProductThumbnailUploadTask>();
        foreach (var thumbnail in _uploadSessionContext.ProductDraft.UpcastThumbnailsTo<TurboSquid3DProductThumbnail>())
            thumbnailsUploadTasks.Add(await CreateThumbnailUploadTaskAsync(thumbnail, cancellationToken));

        (_uploadSessionContext.ProductDraft._Product.Metadata as TurboSquid3DProductMetadata)!.UploadedThumbnails
            .AddRange(await AwaitThumbnailsUploadTasksAsync(thumbnailsUploadTasks, cancellationToken));
    }

    /// <returns>
    /// The object functionally similar to <see cref="Task"/> that must be "awaited" by calling
    /// <see cref="AwaitThumbnailsUploadTasksAsync(IList{TurboSquid3DProductThumbnailUploadTask}, CancellationToken)"/>
    /// passing it all of these objects in bulk.
    /// </returns>
    async Task<TurboSquid3DProductThumbnailUploadTask> CreateThumbnailUploadTaskAsync(TurboSquid3DProductThumbnail thumbnail, CancellationToken cancellationToken)
    {
        string uploadKey = await UploadAssetAsyncAt(thumbnail.FilePath, cancellationToken);
        string processingId = await ProcessAssetAsync(thumbnail.ToProcessJsonContentUsing(_uploadSessionContext, uploadKey), cancellationToken);

        return new(thumbnail, processingId);
    }

    async Task<List<TurboSquidUploaded3DProductThumbnail>> AwaitThumbnailsUploadTasksAsync(IList<TurboSquid3DProductThumbnailUploadTask> thumnailsUploadTasks, CancellationToken cancellationToken)
    {
        var uploadedThumbnailsIds = new List<TurboSquidUploaded3DProductThumbnail>(thumnailsUploadTasks.Count);
        while (thumnailsUploadTasks.Any())
        {
            var thumbnailsUploadTasksIDs = thumnailsUploadTasks.Select(task => task.ID);
            foreach (var thumbnailUploadStatus in await PollThumbnailsUploadStatusAsync())
            {
                if ((string)thumbnailUploadStatus["status"]! == "success")
                {
                    var processedThumbnail = thumnailsUploadTasks.Single(task => task.ID == (string)thumbnailUploadStatus["id"]!);
                    uploadedThumbnailsIds.Add(new(processedThumbnail.Thumbnail, (int)thumbnailUploadStatus["file_id"]!));
                    thumnailsUploadTasks.Remove(processedThumbnail);
                }
            }


            async Task<JEnumerable<JToken>> PollThumbnailsUploadStatusAsync()
            {
                var response = await
                    (await _httpClient.PostAsJsonAsync(new Uri(TurboSquidApi._BaseUri, "turbosquid/uploads/bulk_poll"), new
                    {
                        authenticity_token = _uploadSessionContext.Credential._CsrfToken,
                        thumbnailsUploadTasksIDs
                    }, cancellationToken))
                    .EnsureSuccessStatusCode()
                    .Content.ReadAsStringAsync(cancellationToken);

                return JArray.Parse(response).Children();
            }
        }
        return uploadedThumbnailsIds;
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

    /// <returns>The processing ID that is used for monitoring upload status of an asset.</returns>
    async Task<string> ProcessAssetAsync(HttpContent processHttpContent, CancellationToken cancellationToken)
    {
        var response = (await _httpClient.PostAsync(
            new Uri(TurboSquidApi._BaseUri, "turbosquid/uploads//process"),
            processHttpContent,
            cancellationToken)
            ).EnsureSuccessStatusCode();
        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        return (string)JObject.Parse(responseContent)["id"]!;
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
