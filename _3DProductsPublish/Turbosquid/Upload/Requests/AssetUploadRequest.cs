using Microsoft.Net.Http.Headers;

namespace _3DProductsPublish.Turbosquid.Upload.Requests;

internal abstract class AssetUploadRequest
{
    protected readonly Uri UploadEndpoint;
    protected readonly string UnixTimestamp;
    protected readonly TurboSquidAwsUploadCredentials AwsUploadCredentials;
    string UploadKey => UploadEndpoint.AbsolutePath.TrimStart('/');

    internal static async Task<AssetUploadRequest> CreateAsyncFor(FileStream asset, TurboSquid3DProductUploadSessionContext context, CancellationToken cancellationToken)
    {
        int partsCount = (int)Math.Ceiling(asset.Length / (double)MultipartAssetUploadRequest.MaxPartSize);
        return partsCount == 1 ?
            await SinglepartAssetUploadRequest.CreateAsyncFor(asset, context) :
            await MultipartAssetUploadRequest.CreateAsyncFor(asset, context, partsCount);
    }

    protected AssetUploadRequest(Uri uploadEndpoint, string unixTimestamp, TurboSquidAwsUploadCredentials awsUploadCredentials)
    {
        UploadEndpoint = uploadEndpoint;
        UnixTimestamp = unixTimestamp;
        AwsUploadCredentials = awsUploadCredentials;
    }

    /// <returns>The upload key.</returns>
    internal async Task<string> SendAsyncUsing(HttpClient httpClient, CancellationToken cancellationToken)
    {
        await SendAsyncCoreUsing(httpClient, cancellationToken);
        return UploadKey;
    }
    protected abstract Task SendAsyncCoreUsing(HttpClient httpClient, CancellationToken cancellationToken);

    protected HttpRequestMessage OptionsRequestFor(HttpRequestMessage request)
    {
        string assetName = Path.GetFileName(request.RequestUri!.AbsolutePath);
        var requestUri = new UriBuilder(new Uri(UploadEndpoint, assetName)) { Query = request.RequestUri.Query }.Uri;

        var optionsRequest = new HttpRequestMessage(HttpMethod.Options, requestUri);
        optionsRequest.Headers.Add("Access-Control-Request-Headers", string.Join(',', new List<string>(AwsUploadCredentials._XAmzHeadersWith(default).Select(header => header.Key))
        { HeaderNames.Authorization.ToLower() }
        .OrderBy(h => h)));
        optionsRequest.Headers.Add("Access-Control-Request-Method", request.Method.Method);
        optionsRequest.Headers.Add("Origin", "https://www.squid.io");

        return optionsRequest;
    }
}
