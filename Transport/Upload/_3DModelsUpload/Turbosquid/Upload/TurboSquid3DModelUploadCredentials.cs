using Newtonsoft.Json.Linq;

namespace Transport.Upload._3DModelsUpload.Turbosquid.Upload;

internal record TurboSquidAwsUploadCredentials
{
    internal readonly string AccessKey;
    internal readonly string Bucket;
    internal readonly string CurrentServerTime;
    internal readonly string KeyPrefix;
    internal readonly string Region;
    internal readonly string SecretKey;
    internal readonly string SessionToken;

    internal static async Task<TurboSquidAwsUploadCredentials> _AsyncFrom(HttpResponseMessage response)
    {
        var uploadCredentialsJson = JObject.Parse(await response.Content.ReadAsStringAsync());
        return new(
            (string)uploadCredentialsJson["access_key"]!,
            (string)uploadCredentialsJson["bucket"]!,
            (string)uploadCredentialsJson["current_server_time"]!,
            (string)uploadCredentialsJson["key_prefix"]!,
            (string)uploadCredentialsJson["region"]!,
            (string)uploadCredentialsJson["secret_key"]!,
            (string)uploadCredentialsJson["session_token"]!);
    }

    TurboSquidAwsUploadCredentials(
        string accessKey,
        string bucket,
        string currentServerTime,
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
}
