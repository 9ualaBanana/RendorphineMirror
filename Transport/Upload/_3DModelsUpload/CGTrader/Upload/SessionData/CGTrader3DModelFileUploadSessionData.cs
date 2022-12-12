using Newtonsoft.Json.Linq;

namespace Transport.Upload._3DModelsUpload.CGTrader.Upload.SessionData;

internal record CGTrader3DModelFileUploadSessionData : CGTrader3DModelAssetUploadSessionData
{
    internal readonly string _Key;
    internal readonly string _AwsAccessKeyID;
    internal readonly string _Acl;
    internal readonly string _Policy;
    internal readonly string _Signature;
    internal readonly string _SuccessActionStatus;

    internal static async Task<CGTrader3DModelFileUploadSessionData> _AsyncFrom(
        HttpResponseMessage response,
        string modelFilePath,
        CancellationToken cancellationToken)
    {
        var modelFileUploadSessionDataJson = JObject.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var modelFileUploadReceiverStorageDataJson = modelFileUploadSessionDataJson["storage"]!;

        using var modelFileStream = File.OpenRead(modelFilePath);
        var fileBytes = new byte[modelFileStream.Length];
        await modelFileStream.ReadAsync(fileBytes.AsMemory(), cancellationToken);

        return new(
            modelFilePath,
            (string)modelFileUploadSessionDataJson["storageLocation"]!,
            (string)modelFileUploadSessionDataJson["id"]!,
            (string)modelFileUploadReceiverStorageDataJson["key"]!,
            (string)modelFileUploadReceiverStorageDataJson["awsAccessKeyId"]!,
            (string)modelFileUploadReceiverStorageDataJson["acl"]!,
            (string)modelFileUploadReceiverStorageDataJson["policy"]!,
            (string)modelFileUploadReceiverStorageDataJson["signature"]!,
            (string)modelFileUploadReceiverStorageDataJson["success_action_status"]!);
    }

    CGTrader3DModelFileUploadSessionData(
        string modelFilePath,
        string storageLocation,
        string fileId,
        string key,
        string awsAccessKeyId,
        string acl,
        string policy,
        string signature,
        string succcessActionStatus) : this(
            File.OpenRead(modelFilePath),
            storageLocation,
            fileId,
            key,
            awsAccessKeyId,
            acl,
            policy,
            signature,
            succcessActionStatus
            )
    {
    }

    CGTrader3DModelFileUploadSessionData(
        FileStream modelFileStream,
        string storageLocation,
        string fileId,
        string key,
        string awsAccessKeyId,
        string acl,
        string policy,
        string signature,
        string successActionStatus) : base(modelFileStream.Name, storageLocation, fileId)
    {
        _Key = key;
        _AwsAccessKeyID = awsAccessKeyId;
        _Acl = acl;
        _Policy = policy;
        _Signature = signature;
        _SuccessActionStatus = successActionStatus;


        modelFileStream.Close();
    }

    internal override async Task _UseToUploadWith(HttpClient httpClient, HttpMethod httpMethod, CancellationToken cancellationToken)
    {
        using var modelFileStream = File.OpenRead(_FilePath);
        (await httpClient.SendAsync(
            new HttpRequestMessage(httpMethod, _StorageLocation) { Content = _MultipartFormDataForUploadFor(modelFileStream) }, cancellationToken)
        ).EnsureSuccessStatusCode();
    }

    internal HttpContent _MultipartFormDataForUploadFor(FileStream modelFileStream) => _httpContent ??= new MultipartFormDataContent()
    {
        { new StringContent(_Key), "key" },
        { new StringContent(_AwsAccessKeyID), "awsAccessKeyId" },
        { new StringContent(_Acl), "acl" },
        { new StringContent(_Policy), "policy" },
        { new StringContent(_Signature), "signature" },
        { new StringContent(_SuccessActionStatus), "success_action_status" },
        { new StreamContent(modelFileStream), "file", Path.GetFileName(modelFileStream.Name) }
    };
    HttpContent? _httpContent;
}
