using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Upload;
using _3DProductsPublish.Turbosquid.Upload.Processing;
using Microsoft.Net.Http.Headers;
using System.Net.Mime;

namespace _3DProductsPublish.Turbosquid.Api;

internal class TurboSquidPublishApi
{
    readonly TurboSquidUploaded3DProductAssets _uploadedAssets;
    readonly TurboSquid3DProductUploadSessionContext _uploadSessionContext;
    readonly HttpClient _httpClient;

    internal TurboSquidPublishApi(HttpClient httpClient, TurboSquid3DProductUploadSessionContext productUploadSessionContext, TurboSquidUploaded3DProductAssets uploadedAssets)
    {
        _httpClient = httpClient;
        _uploadSessionContext = productUploadSessionContext;
        _uploadedAssets = uploadedAssets;
    }

    internal async Task UploadMetadataAsync(CancellationToken cancellationToken)
    {
        foreach (var uploadedModel in _uploadedAssets.Models)
            await _httpClient.SendAsync(
                new HttpRequestMessage(
                    HttpMethod.Patch,
                    
                    new Uri(TurboSquidApi._BaseUri, $"turbosquid/products/{_uploadSessionContext.ProductDraft._ID}/product_files/{uploadedModel.FileId}"))
                { Content = MetadataFormFor(uploadedModel) },
                cancellationToken);


        StringContent MetadataFormFor(ITurboSquidProcessed3DProductAsset<_3DModel<TurboSquid3DModelMetadata>> processedThumbnail)
        {
            using var archived3DModel = File.OpenRead(processedThumbnail.Asset.ArchiveAsync(CancellationToken.None).Result);
            // Explicit conversions of numbers to strings are required.
            var metadataForm = new JObject(
                new JProperty("authenticity_token", _uploadSessionContext.Credential._CsrfToken),
                new JProperty("draft_id", _uploadSessionContext.ProductDraft._ID),
                new JProperty("file_format", processedThumbnail.Asset.Metadata_.FileFormat),
                new JProperty("format_version", processedThumbnail.Asset.Metadata_.FormatVersion.ToString()),
                new JProperty("id", processedThumbnail.FileId),
                new JProperty("is_native", processedThumbnail.Asset.Metadata_.IsNative),
                new JProperty("name", Path.GetFileName(archived3DModel.Name)),
                new JProperty("product_id", 0.ToString()),
                new JProperty("size", archived3DModel.Length)
                );
            if (processedThumbnail.Asset.Metadata_.Renderer is string renderer)
            {
                metadataForm.Add("renderer", renderer);
                if (processedThumbnail.Asset.Metadata_.RendererVersion is double version)
                    metadataForm.Add("renderer_version", version);
            }

            return metadataForm.ToJsonContent();
        }
    }

    internal async Task PublishProductAsync(CancellationToken cancellationToken)
    {
        var productPublishRequest = new HttpRequestMessage(
            HttpMethod.Patch,
            
            new Uri(TurboSquidApi._BaseUri, "turbosquid/products/0"))
        { Content = ProductForm() };
        productPublishRequest.Headers.Add("Origin", "https://www.squid.io");
        productPublishRequest.Headers.Add(HeaderNames.Accept, MediaTypeNames.Application.Json);

        await _httpClient.SendAsync(productPublishRequest, cancellationToken);


        StringContent ProductForm()
            => new JObject(
                new JProperty("authenticity_token", _uploadSessionContext.Credential._CsrfToken),
                new JProperty("turbosquid_product_form", (_uploadSessionContext.ProductDraft._Product.Metadata as TurboSquid3DProductMetadata)!.ToProductForm(_uploadSessionContext.ProductDraft._ID)),
                new JProperty("previews", new JObject(
                    _uploadedAssets.Thumbnails.Select(_ => new JProperty(
                        _.FileId, JObject.FromObject(new
                        {
                            id = _.FileId,
                            image_type = _.Type().ToString()
                        }))
                    ))),
                new JProperty("feature_ids", new int[] { 191, 27964 }),
                new JProperty("missing_brand", JObject.FromObject(new
                {
                    name = string.Empty,
                    website = string.Empty
                }))
            ).ToJsonContent();
        }
    }
