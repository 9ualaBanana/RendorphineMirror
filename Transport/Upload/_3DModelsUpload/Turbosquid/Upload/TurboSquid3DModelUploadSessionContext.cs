using Transport.Upload._3DModelsUpload.CGTrader.Upload;

namespace Transport.Upload._3DModelsUpload.Turbosquid.Upload;

internal record TurboSquid3DModelUploadSessionContext(
    Composite3DModelDraft _Draft,
    TurboSquidAwsUploadCredentials _Credentials)
{
}
