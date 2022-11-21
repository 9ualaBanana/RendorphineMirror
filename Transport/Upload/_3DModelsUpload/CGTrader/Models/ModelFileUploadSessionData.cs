using System.Net;

namespace Transport.Upload._3DModelsUpload.CGTrader.Models;

internal record ModelFileUploadSessionData(
    FileStream FileStream,
    string ModelDraftID,
    string FileID,
    string StorageHost,
    string Key,
    string AwsAccessKeyId,
    string Acl,
    string Policy,
    string Signature,
    HttpStatusCode SuccesActionStatus)
{
    internal MultipartFormDataContent _AsMultipartFormDataContent =>
        _asMultipartFormDataContent ??= new()
        {
            { new StringContent(Key), "key" },
            { new StringContent(AwsAccessKeyId), "awsAccessKeyId" },
            { new StringContent(Acl), "acl" },
            { new StringContent(Policy), "policy" },
            { new StringContent(Signature), "signature" },
            { new StringContent(SuccesActionStatus.ToString()), "success_action_status" },
            { new StreamContent(FileStream), "file", Path.GetFileName(FileStream.Name) }
        };
    MultipartFormDataContent? _asMultipartFormDataContent;
}
