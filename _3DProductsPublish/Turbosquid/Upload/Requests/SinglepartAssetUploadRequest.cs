namespace _3DProductsPublish.Turbosquid.Upload.Requests;

internal class SinglepartAssetUploadRequest : AssetUploadRequest
{
    readonly HttpRequestMessage _assetUploadRequest;

    new internal static async Task<SinglepartAssetUploadRequest> CreateAsyncFor(
        FileStream asset, TurboSquid.PublishSession session)
    {
        var unixTimestamp = DateTime.UtcNow.AsUnixTimestamp();
        var uploadEndpoint = session.UploadEndpointFor(asset, unixTimestamp);
        var assetUploadRequest = await new HttpRequestMessage(HttpMethod.Put, uploadEndpoint)
        { Content = new StreamContent(asset) }
        .SignAsyncWith(session.AwsCredential, includeAcl: true);

        return new(assetUploadRequest, unixTimestamp, uploadEndpoint, session);
    }

    SinglepartAssetUploadRequest(
        HttpRequestMessage assetUploadRequest,
        string unixTimestamp,
        Uri uploadEndpoint,
        TurboSquid.PublishSession session) : base(uploadEndpoint, unixTimestamp, session)
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
