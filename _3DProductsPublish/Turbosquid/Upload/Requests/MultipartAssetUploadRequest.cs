using Common;
using Direx;
using System.Xml;

namespace _3DProductsPublish.Turbosquid.Upload.Requests;

internal class MultipartAssetUploadRequest : AssetUploadRequest, IDisposable
{
    readonly FileStream _asset;
    readonly HttpRequestMessage _initializingUploadIdRequest;
    readonly int _partsCount;

    internal static async Task<MultipartAssetUploadRequest> CreateAsyncFor(
        FileStream asset,
        TurboSquid3DProductUploadSessionContext uploadSessionContext,
        int partsCount)
    {
        var unixTimestamp = DateTime.UtcNow.AsUnixTimestamp();
        var uploadEndpoint = uploadSessionContext.UploadEndpointFor(asset, unixTimestamp);
        var requestUri = new UriBuilder(uploadEndpoint) { Query = "uploads" }.Uri;
        var initializingUploadIdRequest = await new HttpRequestMessage(HttpMethod.Post, requestUri)
            .SignAsyncWith(uploadSessionContext.AwsUploadCredentials, includeAcl: true);

        return new(asset, initializingUploadIdRequest, unixTimestamp, uploadEndpoint, uploadSessionContext.AwsUploadCredentials, partsCount);
    }

    MultipartAssetUploadRequest(
        FileStream asset,
        HttpRequestMessage initializingUploadIdRequest,
        string unixTimestamp,
        Uri uploadEndpoint,
        TurboSquidAwsUploadCredentials awsUploadCredentials,
        int partsCount) : base(uploadEndpoint, unixTimestamp, awsUploadCredentials)
    {
        _asset = asset;
        _initializingUploadIdRequest = initializingUploadIdRequest;
        _partsCount = partsCount;
    }

    protected override async Task SendAsyncCoreUsing(HttpClient httpClient, CancellationToken cancellationToken)
    {
        string uploadId = await RequestUploadIDAsyncUsing(httpClient, cancellationToken);

        var assetPartsUploadRequests = await
            CreateAssetPartsUploadRequestsAsyncFor(_asset, UploadEndpoint, AwsUploadCredentials, uploadId, _partsCount, cancellationToken);
        var multipartAssetUploadResult = await UploadAssetPartsAsyncUsing(
            httpClient,
            assetPartsUploadRequests,
            cancellationToken);

        await CompleteMultipartUploadAsyncUsing(httpClient, uploadId, multipartAssetUploadResult, cancellationToken);
    }

    async Task<string> RequestUploadIDAsyncUsing(HttpClient httpClient, CancellationToken cancellationToken)
    {
        (await httpClient.SendAsync(OptionsRequestFor(_initializingUploadIdRequest), cancellationToken)).EnsureSuccessStatusCode();
        var response = await
            (await httpClient.SendAsync(_initializingUploadIdRequest, cancellationToken))
            .Content.ReadAsStreamAsync(cancellationToken);

        using var xmlReader = XmlReader.Create(response);
        if (!xmlReader.ReadToFollowing("UploadId")) throw new MissingFieldException("Response doesn't contain UploadId.");
        else return xmlReader.ReadElementContentAsString();
    }

    static async Task<IList<AssetPartUploadRequest>> CreateAssetPartsUploadRequestsAsyncFor(
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
                await AssetPartUploadRequest.CreateAsyncFor(asset, uploadEndpoint, awsUploadCredentials, partNumber, uploadId, cancellationToken)
                );
        return assetPartsUploadRequests;
    }

    async Task<MultipartAssetUploadResult> UploadAssetPartsAsyncUsing(
        HttpClient httpClient,
        IList<AssetPartUploadRequest> assetPartsUploadRequests,
        CancellationToken cancellationToken)
    {
        foreach (var assetPartUploadRequest in assetPartsUploadRequests)
            (await httpClient.SendAsync(OptionsRequestFor(assetPartUploadRequest), cancellationToken))
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

    async Task CompleteMultipartUploadAsyncUsing(HttpClient httpClient, string uploadId, MultipartAssetUploadResult assetMultipartUploadResult, CancellationToken cancellationToken)
    {
        var multipartUploadCompletionRequest = await new HttpRequestMessage(HttpMethod.Post, $"{UploadEndpoint}?uploadId={uploadId}")
        { Content = new StringContent(assetMultipartUploadResult._ToXML()) }
        .SignAsyncWith(AwsUploadCredentials);

        (await httpClient.SendAsync(OptionsRequestFor(multipartUploadCompletionRequest), cancellationToken))
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

        internal static async Task<AssetPartUploadRequest> CreateAsyncFor(
            FileStream asset,
            Uri uploadEndpoint,
            TurboSquidAwsUploadCredentials awsUploadCredentials,
            int partNumber,
            string uploadId,
            CancellationToken cancellationToken)
        {
            var content = new MemoryStream(_MaxPartSize);
            await asset.CopyAtMostAsync(content, _MaxPartSize, cancellationToken: cancellationToken);
            content.Position = 0;
            var requestUri = new UriBuilder(uploadEndpoint) { Query = $"partNumber={partNumber}&uploadId={uploadId}" }.Uri;

            return (AssetPartUploadRequest)await new AssetPartUploadRequest(content, requestUri).SignAsyncWith(awsUploadCredentials);
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
