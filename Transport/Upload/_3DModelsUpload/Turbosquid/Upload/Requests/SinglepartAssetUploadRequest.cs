namespace Transport.Upload._3DModelsUpload.Turbosquid.Upload.Requests;

internal class SinglepartAssetUploadRequest : AssetUploadRequest
{
    readonly HttpRequestMessage _assetUploadRequest;

    internal static async Task<SinglepartAssetUploadRequest> _CreateAsyncFor(
        FileStream asset,
        Uri uploadEndpoint,
        TurboSquidAwsUploadCredentials awsUploadCredentials)
    {
        var assetUploadRequest = await new HttpRequestMessage(HttpMethod.Put, uploadEndpoint)
        { Content = new StreamContent(asset) }
        ._SignAsyncWith(awsUploadCredentials, includeAcl: true);

        return new(assetUploadRequest, uploadEndpoint, awsUploadCredentials);
    }

    SinglepartAssetUploadRequest(
        HttpRequestMessage assetUploadRequest,
        Uri uploadEndpoint,
        TurboSquidAwsUploadCredentials awsUploadCredentials) : base(uploadEndpoint, awsUploadCredentials)
    {
        _assetUploadRequest = assetUploadRequest;
    }

    internal async Task _SendAsyncUsing(HttpClient httpClient, CancellationToken cancellationToken)
    {
        (await httpClient.SendAsync(_OptionsRequestFor(_assetUploadRequest), cancellationToken))
            .EnsureSuccessStatusCode();
        (await httpClient.SendAsync(_assetUploadRequest, cancellationToken))
            .EnsureSuccessStatusCode();
    }
}
