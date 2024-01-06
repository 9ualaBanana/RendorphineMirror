using lvl;
using System.Xml;

namespace _3DProductsPublish.Turbosquid.Upload.Requests;

internal class MultipartAssetUploadRequest : AssetUploadRequest, IDisposable
{
    internal const int MaxPartSize = 5242880;

    readonly FileStream _asset;
    readonly HttpRequestMessage _initializingUploadIdRequest;
    readonly int _partsCount;

    internal static async Task<MultipartAssetUploadRequest> CreateAsyncFor(
        FileStream asset,
        TurboSquid.PublishSession session,
        int partsCount)
    {
        var unixTimestamp = DateTime.UtcNow.AsUnixTimestamp();
        var uploadEndpoint = session.UploadEndpointFor(asset, unixTimestamp);
        var requestUri = new UriBuilder(uploadEndpoint) { Query = "uploads" }.Uri;
        var initializingUploadIdRequest = await new HttpRequestMessage(HttpMethod.Post, requestUri)
            .SignAsyncWith(session.AwsCredential, includeAcl: true);

        return new(asset, initializingUploadIdRequest, unixTimestamp, uploadEndpoint, session, partsCount);
    }

    MultipartAssetUploadRequest(
        FileStream asset,
        HttpRequestMessage initializingUploadIdRequest,
        string unixTimestamp,
        Uri uploadEndpoint,
        TurboSquid.PublishSession session,
        int partsCount) : base(uploadEndpoint, unixTimestamp, session)
    {
        _asset = asset;
        _initializingUploadIdRequest = initializingUploadIdRequest;
        _partsCount = partsCount;
    }

    protected override async Task SendAsyncCore()
    {
        string uploadId = await RequestUploadIDAsync();

        var assetPartsUploadRequests = await
            CreateAssetPartsUploadRequestsAsyncFor(_asset, UploadEndpoint, Session.AwsCredential, uploadId, _partsCount, Session.CancellationToken);
        var multipartAssetUploadResult = await UploadAssetPartsAsyncUsing(assetPartsUploadRequests);

        await CompleteMultipartUploadAsync(uploadId, multipartAssetUploadResult);
    }

    async Task<string> RequestUploadIDAsync()
    {
        (await Session.Client.SendAsync(OptionsRequestFor(_initializingUploadIdRequest), Session.CancellationToken)).EnsureSuccessStatusCode();
        var response = await
            (await Session.Client.SendAsync(_initializingUploadIdRequest, Session.CancellationToken))
            .Content.ReadAsStreamAsync(Session.CancellationToken);

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
        IList<AssetPartUploadRequest> assetPartsUploadRequests)
    {
        foreach (var assetPartUploadRequest in assetPartsUploadRequests)
            (await Session.Client.SendAsync(OptionsRequestFor(assetPartUploadRequest), Session.CancellationToken))
                .EnsureSuccessStatusCode();

        var assetPartsUploadResults = new List<AssetPartUploadResult>(assetPartsUploadRequests.Count);
        int partNumber = 1;
        foreach (var assetPartUploadRequest in assetPartsUploadRequests)
        {
            var uploadedAssetETag = (await Session.Client.SendAsync(assetPartUploadRequest, Session.CancellationToken))
                .EnsureSuccessStatusCode()
                .Headers.ETag!.Tag;
            assetPartUploadRequest.Dispose();
            assetPartsUploadResults.Add(new(partNumber++, uploadedAssetETag));
        }

        return new(assetPartsUploadResults);
    }

    async Task CompleteMultipartUploadAsync(string uploadId, MultipartAssetUploadResult assetMultipartUploadResult)
    {
        var multipartUploadCompletionRequest = await new HttpRequestMessage(HttpMethod.Post, $"{UploadEndpoint}?uploadId={uploadId}")
        { Content = new StringContent(assetMultipartUploadResult._ToXML()) }
        .SignAsyncWith(Session.AwsCredential);

        (await Session.Client.SendAsync(OptionsRequestFor(multipartUploadCompletionRequest), Session.CancellationToken))
            .EnsureSuccessStatusCode();
        (await Session.Client.SendAsync(multipartUploadCompletionRequest, Session.CancellationToken))
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
        readonly MemoryStream _content;

        internal static async Task<AssetPartUploadRequest> CreateAsyncFor(
            FileStream asset,
            Uri uploadEndpoint,
            TurboSquidAwsUploadCredentials awsUploadCredentials,
            int partNumber,
            string uploadId,
            CancellationToken cancellationToken)
        {
            var content = new MemoryStream(MaxPartSize);
            await asset.CopyAtMostAsync(content, MaxPartSize, cancellationToken: cancellationToken);
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
