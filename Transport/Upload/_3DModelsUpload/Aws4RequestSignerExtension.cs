using AwsSignatureVersion4.Private;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Transport.Upload._3DModelsUpload.Turbosquid.Upload;

namespace Transport.Upload._3DModelsUpload;

internal static class Aws4RequestSignerExtension
{
    const string _ServiceName = "s3";

    internal static async Task<HttpRequestMessage> _SignedAsyncWith(this HttpRequestMessage request, TurboSquidAwsUploadCredentials awsCredentials)
    {
        // Date is taken from `awsCredentials` but if the date in the request and current date on the server differ in 1.5 minutes, then the server responds with 403.
        // Also that hack with adding 4 hours to it is not a good idea.
        var requestDateTime = DateTime.Parse(awsCredentials.CurrentServerTime).AddHours(4);
        await Signer.SignAsync(request, default, _XAmzHeadersWith(requestDateTime, awsCredentials), requestDateTime, awsCredentials.Region, _ServiceName, new(awsCredentials.AccessKey, awsCredentials.SecretKey, awsCredentials.SessionToken));
        return request;
    }

    static IEnumerable<KeyValuePair<string, IEnumerable<string>>> _XAmzHeadersWith(DateTime requestDateTime, TurboSquidAwsUploadCredentials awsCredentials) =>
        new KeyValuePair<string, IEnumerable<string>>[]
        {
            new KeyValuePair<string, IEnumerable<string>>("x-amz-acl", "private"._ToHeaderValue()),
            new KeyValuePair<string, IEnumerable<string>>("x-amz-content-sha256", "UNSIGNED-PAYLOAD"._ToHeaderValue()),
            new KeyValuePair<string, IEnumerable<string>>("x-amz-date", requestDateTime.ToIso8601BasicDateTime()._ToHeaderValue()),
            new KeyValuePair<string, IEnumerable<string>>("x-amz-security-token", awsCredentials.SessionToken._ToHeaderValue())
        };

    static IEnumerable<string> _ToHeaderValue(this string value) => new string[] { value };
}
