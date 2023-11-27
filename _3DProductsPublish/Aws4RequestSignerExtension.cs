using _3DProductsPublish.Turbosquid.Upload;
using AwsSignatureVersion4.Private;

namespace _3DProductsPublish;

internal static class Aws4RequestSignerExtension
{
    const string ServiceName = "s3";

    // TODO: refactor `includeAcl`.
    internal static async Task<HttpRequestMessage> SignAsyncWith(this HttpRequestMessage request, TurboSquidAwsUploadCredentials awsCredentials, bool includeAcl = false)
    {
        // If <RequestTime> and <ServerTime> differ in 1.5 minutes the server responds with 403.
        await Signer.SignAsync(request, default, awsCredentials.ToHeaders(includeAcl), awsCredentials.CurrentServerTime, awsCredentials.Region, ServiceName, new(awsCredentials.AccessKey, awsCredentials.SecretKey, awsCredentials.SessionToken), includeAcl);
        return request;
    }
}
