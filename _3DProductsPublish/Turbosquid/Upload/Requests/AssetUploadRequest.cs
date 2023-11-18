using Microsoft.Net.Http.Headers;

namespace _3DProductsPublish.Turbosquid.Upload.Requests;

internal abstract class AssetUploadRequest
{
    protected readonly Uri UploadEndpoint;
    protected readonly string UnixTimestamp;
    protected readonly TurboSquid.PublishSession Session;
    string UploadKey => UploadEndpoint.AbsolutePath.TrimStart('/');

    internal static async Task<AssetUploadRequest> CreateAsyncFor(FileStream asset, TurboSquid.PublishSession session)
    {
        int partsCount = (int)Math.Ceiling(asset.Length / (double)MultipartAssetUploadRequest.MaxPartSize);
        return partsCount == 1 ?
            await SinglepartAssetUploadRequest.CreateAsyncFor(asset, session) :
            await MultipartAssetUploadRequest.CreateAsyncFor(asset, session, partsCount);
    }

    protected AssetUploadRequest(Uri uploadEndpoint, string unixTimestamp, TurboSquid.PublishSession session)
    {
        UploadEndpoint = uploadEndpoint;
        UnixTimestamp = unixTimestamp;
        Session = session;
    }

    /// <returns>The upload key.</returns>
    internal async Task<string> SendAsync()
    {
        await SendAsyncCore();
        return UploadKey;
    }
    protected abstract Task SendAsyncCore();

    protected HttpRequestMessage OptionsRequestFor(HttpRequestMessage request)
    {
        string assetName = Path.GetFileName(request.RequestUri!.AbsolutePath);
        var requestUri = new UriBuilder(new Uri(UploadEndpoint, assetName)) { Query = request.RequestUri.Query }.Uri;

        var optionsRequest = new HttpRequestMessage(HttpMethod.Options, requestUri);
        optionsRequest.Headers.Add("Access-Control-Request-Headers", string.Join(',', new List<string>(Session.AwsCredential._XAmzHeadersWith(default).Select(header => header.Key))
        { HeaderNames.Authorization.ToLower() }
        .OrderBy(h => h)));
        optionsRequest.Headers.Add("Access-Control-Request-Method", request.Method.Method);
        optionsRequest.Headers.Add("Origin", "https://www.squid.io");

        return optionsRequest;
    }
}
