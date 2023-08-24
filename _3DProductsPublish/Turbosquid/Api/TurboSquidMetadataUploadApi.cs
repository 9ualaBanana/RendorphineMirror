using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Upload;
using Microsoft.Net.Http.Headers;
using System.Net.Mime;
using Tomlyn;

namespace _3DProductsPublish.Turbosquid.Api;

internal class TurboSquidMetadataUploadApi
{
    readonly string _metadataFilePath;
    readonly TurboSquid3DProductUploadSessionContext _productUploadSessionContext;
    readonly HttpClient _httpClient;

    const string MetadataFileName = "turbosquid.meta";

    internal TurboSquidMetadataUploadApi(HttpClient httpClient, TurboSquid3DProductUploadSessionContext productUploadSessionContext)
    {
        _metadataFilePath = Path.Combine(productUploadSessionContext.ProductDraft._Product.ContainerPath, MetadataFileName);
        _productUploadSessionContext = productUploadSessionContext;
        _httpClient = httpClient;
    }

    internal async Task UploadAsync(TurboSquidUploaded3DProductAssets uploadedAssets, CancellationToken cancellationToken)
    {
        await UploadModelsMetadataAsync();
        await PublishProductAsync();


        async Task UploadModelsMetadataAsync()
        {
            var modelsMetadata = Toml.Parse(File.ReadAllText(_metadataFilePath))
                .Tables
                .Select(TurboSquid3DModelMetadata.Read)
                .ToDictionary(modelMeta => uploadedAssets.Models.Single(model => Path.GetFileNameWithoutExtension(model.Asset.OriginalPath) == modelMeta.Name));

            if (modelsMetadata.Count != _productUploadSessionContext.ProductDraft._Product._3DModels.Count())
                throw new InvalidDataException($"Metadata file doesn't describe every model of {nameof(_3DProduct)} ({_metadataFilePath}).");

            foreach (var modelMetadata in modelsMetadata)
            {
                using var archived3DModel = File.OpenRead(await modelMetadata.Key.Asset.ArchiveAsync(CancellationToken.None));
                // Explicit conversions of numbers to strings are required.
                var payload = new JObject(
                    new JProperty("authenticity_token", _productUploadSessionContext.Credential._CsrfToken),
                    new JProperty("draft_id", _productUploadSessionContext.ProductDraft._ID),
                    new JProperty("file_format", modelMetadata.Value.FileFormat),
                    new JProperty("format_version", modelMetadata.Value.FormatVersion.ToString()),
                    new JProperty("id", modelMetadata.Key.FileId),
                    new JProperty("is_native", modelMetadata.Value.IsNative),
                    new JProperty("name", Path.GetFileName(archived3DModel.Name)),
                    new JProperty("product_id", 0.ToString()),
                    new JProperty("size", archived3DModel.Length)
                    );
                if (modelMetadata.Value.Renderer is string renderer)
                {
                    payload.Add("renderer", renderer);
                    if (modelMetadata.Value.RendererVersion is double version)
                        payload.Add("renderer_version", version);
                }

                var request = new HttpRequestMessage(HttpMethod.Patch, new Uri(TurboSquidApi._BaseUri, $"turbosquid/products/{_productUploadSessionContext.ProductDraft._ID}/product_files/{modelMetadata.Key.FileId}"))
                { Content = payload.ToJsonContent() };

                await _httpClient.SendAsync(request, cancellationToken);
            }
        }

        async Task PublishProductAsync()
        {
            var productMetadata = new JObject(
                new JProperty("authenticity_token", _productUploadSessionContext.Credential._CsrfToken),
                new JProperty("turbosquid_product_form", (_productUploadSessionContext.ProductDraft._Product.Metadata as TurboSquid3DProductMetadata)!.ToProductForm(_productUploadSessionContext.ProductDraft._ID)),
                new JProperty("previews", new JObject(
                    uploadedAssets.Thumbnails.Select(_ => new JProperty(
                        _.FileId, JObject.FromObject(new
                        {
                            id = _.FileId,
                            image_type = new TurboSquid3DProductUploadedThumbnail(_.Asset, _.FileId).Type.ToString()
                        }))
                    ))),
                new JProperty("feature_ids", new int[] { 191, 27964 }),
                new JProperty("missing_brand", JObject.FromObject(new
                {
                    name = string.Empty,
                    website = string.Empty
                }))
            );

            var request = new HttpRequestMessage(HttpMethod.Patch, new Uri(TurboSquidApi._BaseUri, "turbosquid/products/0"))
            { Content = productMetadata.ToJsonContent() };
            request.Headers.Add("Origin", "https://www.squid.io");
            request.Headers.Add(HeaderNames.Accept, MediaTypeNames.Application.Json);

            await _httpClient.SendAsync(request, cancellationToken);
        }
    }

    JObject ProductForm => new(
            new JProperty("authenticity_token", _productUploadSessionContext.Credential._CsrfToken),
            new JProperty("turbosquid_product_form", (_productUploadSessionContext.ProductDraft._Product.Metadata as TurboSquid3DProductMetadata)!.ToProductForm(_productUploadSessionContext.ProductDraft._ID))
        );
}
