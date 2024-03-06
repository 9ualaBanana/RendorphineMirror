using AwsSignatureVersion4.Private;

namespace _3DProductsPublish.Turbosquid.Upload;

internal record TurboSquidAwsSession
{
    internal readonly string AccessKey;
    /// <summary>
    /// Container for objects stored in Amazon S3 and is the subdomain of that storage URL.
    /// </summary>
    internal readonly string Bucket;
    internal readonly DateTime CurrentServerTime;
    /// <remarks>
    /// Ends with a path separator '/'.
    /// </remarks>
    internal readonly string KeyPrefix;
    internal readonly string Region;
    internal readonly string SecretKey;
    internal readonly string SessionToken;

    internal static TurboSquidAwsSession Parse(string response)
    {
        var uploadCredentialsJson = JObject.Parse(response);
        return new(
            (string)uploadCredentialsJson["access_key"]!,
            (string)uploadCredentialsJson["bucket"]!,
            uploadCredentialsJson["current_server_time"]!.ToObject<DateTime>(),
            (string)uploadCredentialsJson["key_prefix"]!,
            (string)uploadCredentialsJson["region"]!,
            (string)uploadCredentialsJson["secret_key"]!,
            (string)uploadCredentialsJson["session_token"]!);
    }

    TurboSquidAwsSession(
        string accessKey,
        string bucket,
        DateTime currentServerTime,
        string keyPrefix,
        string region,
        string secretKey,
        string sessionToken)
    {
        AccessKey = accessKey;
        Bucket = bucket;
        CurrentServerTime = currentServerTime;
        KeyPrefix = keyPrefix;
        Region = region;
        SecretKey = secretKey;
        SessionToken = sessionToken;
    }

    internal IEnumerable<KeyValuePair<string, IEnumerable<string>>> ToHeaders(bool includeAcl = true)
    {
        if (includeAcl) return new KeyValuePair<string, IEnumerable<string>>[]
        {
            new("x-amz-acl", "private".ToHeaderValue()),
            new("x-amz-content-sha256", "UNSIGNED-PAYLOAD".ToHeaderValue()),
            new("x-amz-date", CurrentServerTime.ToIso8601BasicDateTime().ToHeaderValue()),
            new("x-amz-security-token", SessionToken.ToHeaderValue())
        };
        else return new KeyValuePair<string, IEnumerable<string>>[]
        {
            new("x-amz-content-sha256", "UNSIGNED-PAYLOAD".ToHeaderValue()),
            new("x-amz-date", CurrentServerTime.ToIso8601BasicDateTime().ToHeaderValue()),
            new("x-amz-security-token", SessionToken.ToHeaderValue())
        };
    }

    internal Uri UploadEndpointFor(FileStream asset, string unixTimestamp) =>
        new(new Uri(new($"https://{Bucket}.s3.amazonaws.com/{KeyPrefix}"), unixTimestamp + '/'), Path.GetFileName(asset.Name));
}

static class HeaderExtensions
{
    internal static IEnumerable<string> ToHeaderValue(this string value) => new string[] { value };
}
