using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Upload;
using System.Net.Mime;
using System.Text;

namespace _3DProductsPublish.Turbosquid.Api;

internal class TurboSquidMetadataUploadApi
{
    readonly TurboSquid3DProductUploadSessionContext _productUploadSessionContext;
    readonly HttpClient _httpClient;

    internal TurboSquidMetadataUploadApi(TurboSquid3DProductUploadSessionContext productUploadSessionContext)
    {
        _productUploadSessionContext = productUploadSessionContext;
        _httpClient = new() { BaseAddress = TurboSquidApi._BaseUri };
    }

    internal async Task UploadAsync(TurboSquidUploaded3DProductAssets uploadedAssets, CancellationToken cancellationToken)
    {
        var productMetadata = new JObject(
            new JProperty("authenticity_token", _productUploadSessionContext.Credential._CsrfToken),
            new JProperty("feature_ids", new int[] { 191, 27964 }),
            new JProperty("missing_brand", JObject.FromObject(new
            {
                name = string.Empty,
                website = string.Empty
            })),
            new JProperty("previews", new JObject(
                uploadedAssets.Thumbnails.Select(_ => new JProperty(
                    _.FileId, JObject.FromObject(new
                    {
                        id = _.FileId,
                        image_type = new TurboSquid3DProductUploadedThumbnail(_.Asset, _.FileId).Type.ToString()
                    }))
                ))
            ),
            new JProperty("turbosquid_product_form", (_productUploadSessionContext.ProductDraft._Product.Metadata as TurboSquid3DProductMetadata)!.ToProductForm(_productUploadSessionContext.ProductDraft._ID))
            );

        var request = new HttpRequestMessage(
            HttpMethod.Patch,

            new Uri(TurboSquidApi._BaseUri, "turbosquid/products/0"))
        { Content = new StringContent(productMetadata.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json) };

        await _httpClient.SendAsync(request, cancellationToken);
    }
}
