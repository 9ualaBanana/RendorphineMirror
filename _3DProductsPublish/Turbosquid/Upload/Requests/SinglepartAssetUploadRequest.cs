namespace _3DProductsPublish.Turbosquid.Upload.Requests;

internal class SinglepartAssetUploadRequest : AssetUploadRequest
{
    readonly HttpRequestMessage _assetUploadRequest;

    internal static async Task<SinglepartAssetUploadRequest> CreateAsyncFor(
        FileStream asset, Uri endpoint, TurboSquid.PublishSession session)
        => new(
            await new HttpRequestMessage(HttpMethod.Put, endpoint)
            { Content = new StreamContent(asset) }
            .SignedAsyncWith(session.Draft.AWS, includeAcl: true),
            endpoint, session);

    SinglepartAssetUploadRequest(HttpRequestMessage assetUploadRequest, Uri endpoint, TurboSquid.PublishSession session)
        : base(endpoint, session)
    {
        _assetUploadRequest = assetUploadRequest;
    }

    protected override async Task SendAsyncCore()
    {
        (await Session.Client.SendAsync(OptionsRequestFor(_assetUploadRequest), Session.CancellationToken))
            .EnsureSuccessStatusCode();
        (await Session.Client.SendAsync(_assetUploadRequest, Session.CancellationToken))
            .EnsureSuccessStatusCode();
    }
}
