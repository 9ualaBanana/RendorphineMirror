using Newtonsoft.Json.Linq;

namespace Transport.Upload._3DModelsUpload.Turbosquid.Upload;

internal record TurboSquid3DModelUploadCredentials
{
    internal readonly string AccessKey;
    internal readonly string SessionToken;
    internal readonly string KeyPrefix;
    internal readonly string Bucket;
    internal readonly string Region;
    internal readonly string CurrentServerTime;

    internal static async Task<TurboSquid3DModelUploadCredentials> _AsyncFrom(HttpResponseMessage response)
    {
        var uploadCredentialsJson = JObject.Parse(await response.Content.ReadAsStringAsync());
        return new(
            (string)uploadCredentialsJson["access_key"]!,
            (string)uploadCredentialsJson["session_token"]!,
            (string)uploadCredentialsJson["key_prefix"]!,
            (string)uploadCredentialsJson["bucket"]!,
            (string)uploadCredentialsJson["region"]!,
            // Consider using DateTimeOffset.
            (string)uploadCredentialsJson["current_server_time"]!);
    }

    TurboSquid3DModelUploadCredentials(
        string accessKey,
        string sessionToken,
        string keyPrefix,
        string bucket,
        string region,
        string currentServerTime)
    {
        AccessKey = accessKey;
        SessionToken = sessionToken;
        KeyPrefix = keyPrefix;
        Bucket = bucket;
        Region = region;
        CurrentServerTime = currentServerTime;
    }
}
