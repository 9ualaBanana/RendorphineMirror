namespace Transport.Upload._3DModelsUpload.Turbosquid.Upload.Requests;

internal class SinglepartAssetUploadRequest : AssetUploadRequest
{
    readonly HttpRequestMessage _assetUploadRequest;

    internal static async Task<SinglepartAssetUploadRequest> CreateAsyncFor(
        FileStream asset, TurboSquid3DProductUploadSessionContext uploadSessionContext)
    {
        var unixTimestamp = DateTime.UtcNow.AsUnixTimestamp();
        var uploadEndpoint = uploadSessionContext.UploadEndpointFor(asset.Name, unixTimestamp);
        var assetUploadRequest = await new HttpRequestMessage(HttpMethod.Put, uploadEndpoint)
        { Content = new StreamContent(asset) }
        .SignAsyncWith(uploadSessionContext.AwsUploadCredentials, includeAcl: true);

        return new(assetUploadRequest, unixTimestamp, uploadEndpoint, uploadSessionContext.AwsUploadCredentials);
    }

    SinglepartAssetUploadRequest(
        HttpRequestMessage assetUploadRequest,
        string unixTimestamp,
        Uri uploadEndpoint,
        TurboSquidAwsUploadCredentials awsUploadCredentials) : base(uploadEndpoint, unixTimestamp, awsUploadCredentials)
    {
        _assetUploadRequest = assetUploadRequest;
    }

    protected override async Task SendAsyncCoreUsing(HttpClient httpClient, CancellationToken cancellationToken)
    {
        (await httpClient.SendAsync(OptionsRequestFor(_assetUploadRequest), cancellationToken))
            .EnsureSuccessStatusCode();
        (await httpClient.SendAsync(_assetUploadRequest, cancellationToken))
            .EnsureSuccessStatusCode();
    }
}
