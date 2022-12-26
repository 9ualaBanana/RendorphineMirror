using System.Net.Http.Json;
using Transport.Upload._3DModelsUpload._3DModelDS;
using Transport.Upload._3DModelsUpload.CGTrader._3DModelComponents;
using Transport.Upload._3DModelsUpload.Turbosquid._3DModelComponents;
using Transport.Upload._3DModelsUpload.Turbosquid.Upload;
using Transport.Upload._3DModelsUpload.Turbosquid.Upload.Requests;

namespace Transport.Upload._3DModelsUpload.Turbosquid.Api;

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

    internal async Task _UploadAssetsAsync(CancellationToken cancellationToken)
    {
        foreach (var _3DModel in _uploadSessionContext.ProductDraft._Product._3DModels)
            await _UploadModelAsync(_3DModel, cancellationToken);

        foreach (var thumbnail in _uploadSessionContext.ProductDraft._UpcastThumbnailsTo<TurboSquid3DProductThumbnail>())
        {
            await _UploadThumbnailAsync(thumbnail, cancellationToken);
            //(modelDraft._Model.Metadata as TurboSquid3DModelPreviewImage)!.UploadedPreviewImagesIDs.Add(uploadedFileId);
        }
    }

    async Task _UploadModelAsync(_3DModel _3DModel, CancellationToken cancellationToken)
    {
        var archived3DModel = await _3DModel.ArchiveAsync(cancellationToken);
        string uploadKey = await _UploadAssetAsync(archived3DModel, cancellationToken);

    }

    async Task _UploadThumbnailAsync(TurboSquid3DProductThumbnail thumbnail, CancellationToken cancellationToken)
    {
        string uploadKey = await _UploadAssetAsync(thumbnail.FilePath, cancellationToken);
        await _ProcessAssetAsync(thumbnail._ToProcessJsonContentUsing(_uploadSessionContext, uploadKey), cancellationToken);
    }

    async Task _ProcessAssetAsync(HttpContent processHttpContent, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(TurboSquidApi._BaseUri, "turbosquid/uploads//process"))
        { Content = processHttpContent };
        request.Headers.Referrer = new(TurboSquidApi._BaseUri, $"turbosquid/drafts/{_uploadSessionContext.ProductDraft._ID}/edit");
        request.Headers.Add("Origin", TurboSquidApi._BaseUri.ToString().TrimEnd('/'));
        (await _httpClient.SendAsync(request, cancellationToken))
            .EnsureSuccessStatusCode();
    }
        //(await _httpClient.PostAsync(
        //    new Uri(TurboSquidApi._BaseUri, "turbosquid/uploads//process"),
        //    processHttpContent,
        //    cancellationToken))
        //    .EnsureSuccessStatusCode();

    async Task<string> _UploadAssetAsync(string assetPath, CancellationToken cancellationToken)
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
    internal static JsonContent _ToProcessJsonContentUsing(this _3DModel _3DModel, TurboSquid3DProductUploadSessionContext uploadSessionContext, string uploadKey)
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
                //file_format = null// Get file_format string based on the file extension,
                format_version = string.Empty,
                renderer = string.Empty,
                renderer_version = string.Empty,
                is_native = false
            },
            authenticity_token = uploadSessionContext.Credential._CsrfToken
        });
    }
}
