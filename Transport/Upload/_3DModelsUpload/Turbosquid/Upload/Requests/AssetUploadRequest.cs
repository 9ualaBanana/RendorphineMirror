using Microsoft.Net.Http.Headers;

namespace Transport.Upload._3DModelsUpload.Turbosquid.Upload.Requests;

internal abstract class AssetUploadRequest
{
    protected readonly Uri UploadEndpoint;
    protected readonly TurboSquidAwsUploadCredentials AwsUploadCredentials;

    protected AssetUploadRequest(Uri uploadEndpoint, TurboSquidAwsUploadCredentials awsUploadCredentials)
    {
        UploadEndpoint = uploadEndpoint;
        AwsUploadCredentials = awsUploadCredentials;
    }

    protected HttpRequestMessage _OptionsRequestFor(HttpRequestMessage request)
    {
        string assetName = Path.GetFileName(request.RequestUri!.AbsolutePath);
        var requestUri = new UriBuilder(new Uri(UploadEndpoint, assetName)) { Query = request.RequestUri.Query }.Uri;

        var optionsRequest = new HttpRequestMessage(HttpMethod.Options, requestUri);
        optionsRequest.Headers.Add("Access-Control-Request-Headers", string.Join(',', new List<string>(AwsUploadCredentials._XAmzHeadersWith(default).Select(header => header.Key))
        {
            HeaderNames.Authorization.ToLower(),
            HeaderNames.ContentType.ToLower()
        }.OrderBy(h => h)));
        optionsRequest.Headers.Add("Access-Control-Request-Method", request.Method.Method);
        optionsRequest.Headers.Add("Origin", "https://www.squid.io");

        return optionsRequest;
    }
}
