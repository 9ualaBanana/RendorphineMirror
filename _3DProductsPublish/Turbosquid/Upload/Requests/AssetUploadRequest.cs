using AwsSignatureVersion4.Private;
using Microsoft.Net.Http.Headers;

namespace _3DProductsPublish.Turbosquid.Upload.Requests;

internal abstract class AssetUploadRequest
{
    protected readonly Uri UploadEndpoint;
    protected readonly TurboSquid.PublishSession Session;
    string UploadKey => UploadEndpoint.AbsolutePath.TrimStart('/');

    internal static async Task<AssetUploadRequest> CreateAsyncFor(FileStream asset, TurboSquid.PublishSession session)
    {
        int partsCount = (int)Math.Ceiling(asset.Length / (double)MultipartAssetUploadRequest.MaxPartSize);
        var endpoint = session.AWS.UploadEndpointFor(asset, DateTime.UtcNow.AsUnixTimestamp());
        return partsCount == 1 ?
            await SinglepartAssetUploadRequest.CreateAsyncFor(asset, endpoint, session) :
            await MultipartAssetUploadRequest.CreateAsyncFor(asset, endpoint, session, partsCount);
    }

    protected AssetUploadRequest(Uri uploadEndpoint, TurboSquid.PublishSession session)
    {
        UploadEndpoint = uploadEndpoint;
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
        optionsRequest.Headers.Add("Access-Control-Request-Headers", string.Join(',', new List<string>(Session.AWS.ToHeaders().Select(header => header.Key))
        { HeaderNames.Authorization.ToLower() }
        .OrderBy(h => h)));
        optionsRequest.Headers.Add("Access-Control-Request-Method", request.Method.Method);
        optionsRequest.Headers.Add("Origin", "https://www.squid.io");

        return optionsRequest;
    }
}

static class Aws4RequestSignerExtension
{
    const string ServiceName = "s3";

    // TODO: refactor `includeAcl`.
    internal static async Task<HttpRequestMessage> SignedAsyncWith(this HttpRequestMessage request, TurboSquidAwsSession awsSession, bool includeAcl = false)
    {
        // If <RequestTime> and <ServerTime> differ in 1.5 minutes the server responds with 403.
        await Signer.SignAsync(request, default, awsSession.ToHeaders(includeAcl), awsSession.CurrentServerTime, awsSession.Region, ServiceName, new(awsSession.AccessKey, awsSession.SecretKey, awsSession.SessionToken), includeAcl);
        return request;
    }
}
