using Microsoft.Net.Http.Headers;
using System.Xml;
using Transport.Models;
using Transport.Upload._3DModelsUpload.CGTrader._3DModelComponents;
using Transport.Upload._3DModelsUpload.CGTrader.Upload;
using Transport.Upload._3DModelsUpload.Turbosquid._3DModelComponents;
using Transport.Upload._3DModelsUpload.Turbosquid.Upload;

namespace Transport.Upload._3DModelsUpload.Turbosquid.Api;

internal class TurboSquidUploadApi : IBaseAddressProvider
{
    const int _MaxPartSize = 5242880;

    readonly HttpClient _httpClient;
    readonly TurboSquidAwsUploadCredentials _awsCredentials;

    public string BaseAddress => _baseAddress;
    readonly string _baseAddress;

    internal TurboSquidUploadApi(HttpClient httpClient, TurboSquidAwsUploadCredentials awsCredentials)
    {
        _httpClient = httpClient;
        _awsCredentials = awsCredentials;
        _baseAddress = $"https://{_awsCredentials.Bucket}.s3.amazonaws.com/{_awsCredentials.KeyPrefix}{DateTime.UtcNow.AsUnixTimestamp()}/";
    }

    internal async Task _UploadAssetsAsync(Composite3DModelDraft modelDraft, CancellationToken cancellationToken)
    {
        foreach (var _3DModel in modelDraft._Model._3DModels)
            foreach (var modelFilePath in _3DModel.Files)
                await _UploadAssetAsync(modelFilePath, modelDraft._DraftID, cancellationToken);

        foreach (var modelPreviewImage in modelDraft._UpcastPreviewImagesTo<TurboSquid3DModelPreviewImage>())
        {
            await _UploadAssetAsync(modelPreviewImage.FilePath, modelDraft._DraftID, cancellationToken);
            //(modelDraft._Model.Metadata as TurboSquid3DModelPreviewImage)!.UploadedPreviewImagesIDs.Add(uploadedFileId);
        }
    }

    async Task _UploadAssetAsync(string assetPath, string productDraftId, CancellationToken cancellationToken)
    {
        await _UploadAssetAsyncCore(assetPath, cancellationToken);
    }

    async Task _UploadAssetAsyncCore(string assetPath, CancellationToken cancellationToken)
    {
        using var asset = File.OpenRead(assetPath);
        int partsCount = (int)Math.Ceiling(asset.Length / (double)_MaxPartSize);

        if (partsCount == 1) await _UploadAssetAsSinglepartAsync(asset, cancellationToken);
        else await _UploadAssetAsMultipartAsync(asset, partsCount, cancellationToken);
    }

    async Task _UploadAssetAsSinglepartAsync(FileStream asset, CancellationToken cancellationToken)
    {
        var assetUploadRequest = await new HttpRequestMessage(HttpMethod.Put, _UploadUriWithoutQueryFor(asset.Name))
        { Content = new StreamContent(asset) }
        ._SignAsyncWith(_awsCredentials, includeAcl: true);

        (await _httpClient.SendAsync(_OptionsRequestFor(assetUploadRequest), cancellationToken))
            .EnsureSuccessStatusCode();
        (await _httpClient.SendAsync(assetUploadRequest, cancellationToken))
            .EnsureSuccessStatusCode();
    }

    async Task _UploadAssetAsMultipartAsync(FileStream asset, int partsCount, CancellationToken cancellationToken)
    {
        var uploadId = await _RequestUploadIDAsyncFor(asset.Name, cancellationToken);

        for (int partNumber = 1; partNumber <= partsCount; partNumber++)
            (await _httpClient.SendAsync(
                _OptionsRequest(asset.Name, HttpMethod.Put, _QueryForPartRequestWith(partNumber, uploadId)), cancellationToken))
                .EnsureSuccessStatusCode();

        var assetPartUploadResults = new List<AssetPartUploadResult>(partsCount);
        for (int partNumber = 1; partNumber <= partsCount; partNumber++)
            assetPartUploadResults.Add(await _UploadAssetPartAsync(asset, partNumber, uploadId, cancellationToken));

        await _CompleteMultipartUploadAsyncFor(asset.Name, uploadId, new(assetPartUploadResults), cancellationToken);
    }

    async Task<string> _RequestUploadIDAsyncFor(string assetPath, CancellationToken cancellationToken)
    {
        var uploadIdRequest = await new HttpRequestMessage(HttpMethod.Post, $"{_UploadUriWithoutQueryFor(assetPath)}?uploads")
            ._SignAsyncWith(_awsCredentials, includeAcl: true);

        (await _httpClient.SendAsync(_OptionsRequestFor(uploadIdRequest), cancellationToken)).EnsureSuccessStatusCode();
        
        var response = await
            (await _httpClient.SendAsync(uploadIdRequest, cancellationToken))
            .Content.ReadAsStreamAsync(cancellationToken);

        using var xmlReader = XmlReader.Create(response);
        if (!xmlReader.ReadToFollowing("UploadId")) throw new MissingFieldException("Response doesn't contain UploadId.");
        else return xmlReader.ReadElementContentAsString();
    }

    async Task<AssetPartUploadResult> _UploadAssetPartAsync(FileStream asset, int partNumber, string uploadId, CancellationToken cancellationToken)
    {
        using var assetPart = new MemoryStream(_MaxPartSize);
        await asset._CopyToAsync(assetPart, _MaxPartSize, cancellationToken); assetPart.Position = 0;

        var partUploadRequest = await new HttpRequestMessage(
            HttpMethod.Put,
            (this as IBaseAddressProvider).Endpoint($"/{Path.GetFileName(asset.Name)}?{_QueryForPartRequestWith(partNumber, uploadId)}"))
            { Content = new StreamContent(assetPart) }
            ._SignAsyncWith(_awsCredentials);

        string uploadedAssetETag = (await _httpClient.SendAsync(partUploadRequest, cancellationToken))
            .EnsureSuccessStatusCode().Headers.ETag!.Tag;

        return new(partNumber, uploadedAssetETag);
    }

    async Task _CompleteMultipartUploadAsyncFor(string assetPath, string uploadId, AssetMultipartUploadResult assetMultipartUploadResult, CancellationToken cancellationToken)
    {
        var multipartUploadCompletionRequest = await new HttpRequestMessage(HttpMethod.Post, $"{_UploadUriWithoutQueryFor(assetPath)}?uploadId={uploadId}")
        { Content = new StringContent(assetMultipartUploadResult._ToXML()) }
        ._SignAsyncWith(_awsCredentials);

        (await _httpClient.SendAsync(_OptionsRequestFor(multipartUploadCompletionRequest), cancellationToken))
            .EnsureSuccessStatusCode();
        (await _httpClient.SendAsync(multipartUploadCompletionRequest, cancellationToken))
            .EnsureSuccessStatusCode();
    }

    HttpRequestMessage _OptionsRequest(string assetPath, HttpMethod accessControlRequestMethod, string? queryString = null)
    {
        var requestUri = _UploadUriWithoutQueryFor(assetPath); if (queryString is not null) requestUri += $"?{queryString}";
        var request = new HttpRequestMessage(HttpMethod.Options, requestUri);
        request.Headers.Add("Access-Control-Request-Headers", string.Join(',', new List<string>(_awsCredentials._XAmzHeadersWith(default).Select(header => header.Key))
        {
            HeaderNames.Authorization.ToLower(),
            HeaderNames.ContentType.ToLower()
        }.OrderBy(h => h)));
        request.Headers.Add("Access-Control-Request-Method", accessControlRequestMethod.Method);
        request.Headers.Add("Origin", "https://www.squid.io");

        return request;
    }

    HttpRequestMessage _OptionsRequestFor(HttpRequestMessage requestToPrecedeWithOptions)
    {
        string assetName = Path.GetFileName(requestToPrecedeWithOptions.RequestUri!.AbsolutePath);
        var requestUri = new UriBuilder(_UploadUriWithoutQueryFor(assetName)) { Query = requestToPrecedeWithOptions.RequestUri.Query }.Uri;

        var request = new HttpRequestMessage(HttpMethod.Options, requestUri);
        request.Headers.Add("Access-Control-Request-Headers", string.Join(',', new List<string>(_awsCredentials._XAmzHeadersWith(default).Select(header => header.Key))
        {
            HeaderNames.Authorization.ToLower(),
            HeaderNames.ContentType.ToLower()
        }.OrderBy(h => h)));
        request.Headers.Add("Access-Control-Request-Method", requestToPrecedeWithOptions.Method.Method);
        request.Headers.Add("Origin", "https://www.squid.io");

        return request;
    }

    string _UploadUriWithoutQueryFor(string assetPath) => (this as IBaseAddressProvider).Endpoint($"{Path.GetFileName(assetPath)}");

    string _QueryForPartRequestWith(int partNumber, string uploadId) => $"partNumber={partNumber}&uploadId={uploadId}";
}

static class StreamExtensions
{
    internal static async Task _CopyToAsync(this Stream origin, Stream destination, int bytesToCopy, CancellationToken cancellationToken)
    {
        byte[] buffer;
        int totalBytesRead = 0;
        int bytesRead;
        while (totalBytesRead < bytesToCopy)
        {
            buffer = new byte[81920];
            totalBytesRead += bytesRead = await origin.ReadAsync(buffer, cancellationToken);
            await destination.WriteAsync(buffer, cancellationToken);
            if (bytesRead == 0) break;
        }
    }
}
