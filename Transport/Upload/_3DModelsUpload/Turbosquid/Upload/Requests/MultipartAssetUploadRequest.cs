using System.Xml;

namespace Transport.Upload._3DModelsUpload.Turbosquid.Upload.Requests;

internal class MultipartAssetUploadRequest : AssetUploadRequest, IDisposable
{
    readonly FileStream _asset;
    readonly HttpRequestMessage _initializingUploadIdRequest;

    internal static async Task<MultipartAssetUploadRequest> _CreateAsyncFor(
        FileStream asset,
        Uri uploadEndpoint,
        TurboSquidAwsUploadCredentials awsUploadCredentials)
    {
        var requestUri = new UriBuilder(uploadEndpoint) { Query = "uploads" }.Uri;
        var initializingUploadIdRequest = await new HttpRequestMessage(HttpMethod.Post, requestUri)
            ._SignAsyncWith(awsUploadCredentials, includeAcl: true);

        return new(asset, initializingUploadIdRequest, uploadEndpoint, awsUploadCredentials);
    }

    MultipartAssetUploadRequest(
        FileStream asset,
        HttpRequestMessage initializingUploadIdRequest,
        Uri uploadEndpoint,
        TurboSquidAwsUploadCredentials awsUploadCredentials) : base(uploadEndpoint, awsUploadCredentials)
    {
        _asset = asset;
        _initializingUploadIdRequest = initializingUploadIdRequest;
    }

    internal async Task _SendAsyncUsing(HttpClient httpClient, int partsCount, CancellationToken cancellationToken)
    {
        string uploadId = await _RequestUploadIDAsyncUsing(httpClient, cancellationToken);

        var assetPartsUploadRequests = await AssetPartUploadRequest
            ._CreateAllAsyncFor(_asset, UploadEndpoint, AwsUploadCredentials, uploadId, partsCount, cancellationToken
            );
        var multipartAssetUploadResult = await _UploadAssetPartsAsyncUsing(
            httpClient,
            assetPartsUploadRequests,
            cancellationToken);

        await _CompleteMultipartUploadAsyncUsing(httpClient, uploadId, multipartAssetUploadResult, cancellationToken);
    }

    async Task<string> _RequestUploadIDAsyncUsing(HttpClient httpClient, CancellationToken cancellationToken)
    {
        (await httpClient.SendAsync(_OptionsRequestFor(_initializingUploadIdRequest), cancellationToken)).EnsureSuccessStatusCode();
        var response = await
            (await httpClient.SendAsync(_initializingUploadIdRequest, cancellationToken))
            .Content.ReadAsStreamAsync(cancellationToken);

        using var xmlReader = XmlReader.Create(response);
        if (!xmlReader.ReadToFollowing("UploadId")) throw new MissingFieldException("Response doesn't contain UploadId.");
        else return xmlReader.ReadElementContentAsString();
    }

    async Task<MultipartAssetUploadResult> _UploadAssetPartsAsyncUsing(
        HttpClient httpClient,
        IList<AssetPartUploadRequest> assetPartsUploadRequests,
        CancellationToken cancellationToken)
    {
        foreach (var assetPartUploadRequest in assetPartsUploadRequests)
            (await httpClient.SendAsync(_OptionsRequestFor(assetPartUploadRequest), cancellationToken))
                .EnsureSuccessStatusCode();

        var assetPartsUploadResults = new List<AssetPartUploadResult>(assetPartsUploadRequests.Count);
        int partNumber = 1;
        foreach (var assetPartUploadRequest in assetPartsUploadRequests)
        {
            var uploadedAssetETag = (await httpClient.SendAsync(assetPartUploadRequest, cancellationToken))
                .EnsureSuccessStatusCode()
                .Headers.ETag!.Tag;
            assetPartUploadRequest.Dispose();
            assetPartsUploadResults.Add(new(partNumber++, uploadedAssetETag));
        }

        return new(assetPartsUploadResults);
    }

    async Task _CompleteMultipartUploadAsyncUsing(HttpClient httpClient, string uploadId, MultipartAssetUploadResult assetMultipartUploadResult, CancellationToken cancellationToken)
    {
        var multipartUploadCompletionRequest = await new HttpRequestMessage(HttpMethod.Post, $"{UploadEndpoint}?uploadId={uploadId}")
        { Content = new StringContent(assetMultipartUploadResult._ToXML()) }
        ._SignAsyncWith(AwsUploadCredentials);

        (await httpClient.SendAsync(_OptionsRequestFor(multipartUploadCompletionRequest), cancellationToken))
            .EnsureSuccessStatusCode();
        (await httpClient.SendAsync(multipartUploadCompletionRequest, cancellationToken))
            .EnsureSuccessStatusCode();
    }

    public void Dispose()
    { Dispose(true); GC.SuppressFinalize(this); }

    protected virtual void Dispose(bool managed)
    {
        if (_isDisposed) return;

        _asset?.Dispose();

        _isDisposed = true;
    }
    bool _isDisposed;

    class AssetPartUploadRequest : HttpRequestMessage
    {
        const int _MaxPartSize = 5242880;

        readonly MemoryStream _content;

        internal static async Task<IList<AssetPartUploadRequest>> _CreateAllAsyncFor(
            FileStream asset,
            Uri uploadEndpoint,
            TurboSquidAwsUploadCredentials awsUploadCredentials,
            string uploadId,
            int partsCount,
            CancellationToken cancellationToken)
        {
            var assetPartsUploadRequests = new List<AssetPartUploadRequest>(partsCount);
            for (int partNumber = 1; partNumber <= partsCount; partNumber++)
                assetPartsUploadRequests.Add(
                    await AssetPartUploadRequest._CreateAsyncFor(asset, uploadEndpoint, awsUploadCredentials, partNumber, uploadId, cancellationToken)
                    );
            return assetPartsUploadRequests;
        }

        internal static async Task<AssetPartUploadRequest> _CreateAsyncFor(
            FileStream asset,
            Uri uploadEndpoint,
            TurboSquidAwsUploadCredentials awsUploadCredentials,
            int partNumber,
            string uploadId,
            CancellationToken cancellationToken)
        {
            var content = new MemoryStream(_MaxPartSize);
            await asset._CopyToAsync(content, _MaxPartSize, cancellationToken);
            content.Position = 0;
            var requestUri = new UriBuilder(uploadEndpoint) { Query = $"partNumber={partNumber}&uploadId={uploadId}" }.Uri;

            return (AssetPartUploadRequest) await new AssetPartUploadRequest(content, requestUri)._SignAsyncWith(awsUploadCredentials);
        }

        AssetPartUploadRequest(MemoryStream content, Uri requestUri)
            : base(HttpMethod.Put, requestUri)
        {
            _content = content;
            Content = new StreamContent(content);

        }

        protected override void Dispose(bool _)
        {
            if (_isDisposed) return;

            _content?.Dispose();

            _isDisposed = true;
        }
        bool _isDisposed;
    }
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
