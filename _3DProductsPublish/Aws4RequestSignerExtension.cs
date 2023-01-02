using _3DProductsPublish.Turbosquid.Upload;
using AwsSignatureVersion4.Private;

namespace _3DProductsPublish;

internal static class Aws4RequestSignerExtension
{
    const string _ServiceName = "s3";

    // TODO: refactor `includeAcl`.
    internal static async Task<HttpRequestMessage> SignAsyncWith(this HttpRequestMessage request, TurboSquidAwsUploadCredentials awsCredentials, bool includeAcl = false)
    {
        // Date is taken from `awsCredentials` but if the date in the request and current date on the server differ in 1.5 minutes, then the server responds with 403.
        // Also that hack with adding 4 hours to it is not a good idea.
        var requestDateTime = DateTime.Parse(awsCredentials.CurrentServerTime).AddHours(4);
        await Signer.SignAsync(request, default, awsCredentials._XAmzHeadersWith(requestDateTime, includeAcl), requestDateTime, awsCredentials.Region, _ServiceName, new(awsCredentials.AccessKey, awsCredentials.SecretKey, awsCredentials.SessionToken), includeAcl);
        return request;
    }
}
