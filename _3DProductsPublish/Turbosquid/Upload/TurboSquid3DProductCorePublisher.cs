using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Api;
using _3DProductsPublish.Turbosquid.Upload.Processing;
using _3DProductsPublish.Turbosquid.Upload.Requests;
using Microsoft.Net.Http.Headers;
using System.Net.Mime;

namespace _3DProductsPublish.Turbosquid.Upload;

internal class TurboSquid3DProductCorePublisher
{
    readonly TurboSquid3DProductUploadSessionContext _context;

    public TurboSquid3DProductCorePublisher(TurboSquid3DProductUploadSessionContext context)
    {
        _context = context;
    }

    internal async Task PublishProductAsync(CancellationToken cancellationToken)
    {
        await UploadModelsAsync().ForEachAwaitWithCancellationAsync(UploadMetadataAsync, cancellationToken);
        await FinalizePublishingAsync(await UploadThumbnailsAsync());


        async IAsyncEnumerable<ITurboSquidProcessed3DProductAsset<_3DModel<TurboSquid3DModelMetadata>>> UploadModelsAsync()
        {
            var modelsUpload = await _context.ProductDraft._Product._3DModels
                .Select(async _3DModel =>
                {
                    var archived3DModelPath = await _3DModel.ArchiveAsync(cancellationToken);
                    string uploadKey = await UploadAssetAsyncAt(archived3DModelPath, cancellationToken);
                    return await TurboSquid3DProductAssetProcessing.Task_<_3DModel<TurboSquid3DModelMetadata>>
                        .RunAsync(_3DModel, uploadKey, _context, cancellationToken);
                })
                .RunAsync();
            foreach (var processedAsset in await TurboSquid3DProductAssetProcessing.Task_<_3DModel<TurboSquid3DModelMetadata>>.WhenAll(modelsUpload))
                yield return processedAsset;
        }

        async Task<List<ITurboSquidProcessed3DProductAsset<TurboSquid3DProductThumbnail>>> UploadThumbnailsAsync()
        {
            var thumbnailsUpload = await _context.ProductDraft.UpcastThumbnailsTo<TurboSquid3DProductThumbnail>()
                .Select(async thumbnail =>
                {
                    string uploadKey = await UploadAssetAsyncAt(thumbnail.FilePath, cancellationToken);
                    return await TurboSquid3DProductAssetProcessing.Task_<TurboSquid3DProductThumbnail>
                        .RunAsync(thumbnail, uploadKey, _context, cancellationToken);
                })
                .RunAsync();
            return await TurboSquid3DProductAssetProcessing.Task_<TurboSquid3DProductThumbnail>.WhenAll(thumbnailsUpload);
        }

        async Task<string> UploadAssetAsyncAt(string assetPath, CancellationToken cancellationToken)
        {
            using var asset = File.OpenRead(assetPath);
            int partsCount = (int)Math.Ceiling(asset.Length / (double)MultipartAssetUploadRequest.MaxPartSize);
            return partsCount == 1 ? await UploadAssetAsSinglepartAsync() : await UploadAssetAsMultipartAsync();


            async Task<string> UploadAssetAsSinglepartAsync() => await
                (await SinglepartAssetUploadRequest.CreateAsyncFor(asset, _context))
                .SendAsyncUsing(_context.HttpClient, cancellationToken);

            async Task<string> UploadAssetAsMultipartAsync() => await
                (await MultipartAssetUploadRequest.CreateAsyncFor(asset, _context, partsCount))
                .SendAsyncUsing(_context.HttpClient, cancellationToken);
        }

        async Task UploadMetadataAsync(ITurboSquidProcessed3DProductAsset<_3DModel<TurboSquid3DModelMetadata>> processedModel, CancellationToken cancellationToken)
        {
            await _context.HttpClient.SendAsync(
                new HttpRequestMessage(
                    HttpMethod.Patch,

                    $"turbosquid/products/{_context.ProductDraft._ID}/product_files/{processedModel.FileId}")
                { Content = MetadataForm() },
                cancellationToken);


            StringContent MetadataForm()
            {
                using var archived3DModel = File.OpenRead(processedModel.Asset.ArchiveAsync(CancellationToken.None).Result!);
                // Explicit conversions of numbers to strings are required.
                var metadataForm = new JObject(
                    new JProperty("authenticity_token", _context.Credential._CsrfToken),
                    new JProperty("draft_id", _context.ProductDraft._ID),
                    new JProperty("file_format", processedModel.Asset.Metadata.FileFormat),
                    new JProperty("format_version", processedModel.Asset.Metadata.FormatVersion.ToString()),
                    new JProperty("id", processedModel.FileId),
                    new JProperty("is_native", processedModel.Asset.Metadata.IsNative),
                    new JProperty("name", Path.GetFileName(archived3DModel.Name)),
                    new JProperty("product_id", 0.ToString()),
                    new JProperty("size", archived3DModel.Length)
                    );
                if (processedModel.Asset.Metadata.Renderer is string renderer)
                {
                    metadataForm.Add("renderer", renderer);
                    if (processedModel.Asset.Metadata.RendererVersion is double version)
                        metadataForm.Add("renderer_version", version.ToString());
                }

                return metadataForm.ToJsonContent();
            }
        }

        async Task FinalizePublishingAsync(List<ITurboSquidProcessed3DProductAsset<TurboSquid3DProductThumbnail>> processedThumbnails)
        {
            var productPublishRequest = new HttpRequestMessage(
                HttpMethod.Patch,

                "turbosquid/products/0")
            { Content = ProductForm() };
            productPublishRequest.Headers.Add("Origin", TurboSquidApi.Origin.OriginalString);
            productPublishRequest.Headers.Add(HeaderNames.Accept, MediaTypeNames.Application.Json);

            await _context.HttpClient.SendAsync(productPublishRequest, cancellationToken);


            StringContent ProductForm()
                => new JObject(
                    new JProperty("authenticity_token", _context.Credential._CsrfToken),
                    new JProperty("turbosquid_product_form", _context.ProductDraft._Product.Metadata.ToProductForm(_context.ProductDraft._ID)),
                    new JProperty("previews", new JObject(
                        processedThumbnails.Select(_ => new JProperty(
                            _.FileId, JObject.FromObject(new
                            {
                                id = _.FileId,
                                image_type = _.Type().ToString()
                            }))
                        ))),
                    new JProperty("feature_ids", _context.ProductDraft._Product.Metadata.Features.Values.ToArray()),
                    new JProperty("missing_brand", JObject.FromObject(new
                    {
                        name = string.Empty,
                        website = string.Empty
                    })),
                    new JProperty("publish", string.Empty)
                ).ToJsonContent();
        }
    }
}
