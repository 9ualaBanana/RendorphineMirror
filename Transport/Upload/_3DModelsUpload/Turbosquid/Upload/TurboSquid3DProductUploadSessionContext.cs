using Transport.Upload._3DModelsUpload.CGTrader.Upload;
using Transport.Upload._3DModelsUpload.Turbosquid.Network.Authenticity;

namespace Transport.Upload._3DModelsUpload.Turbosquid.Upload;

internal record TurboSquid3DProductUploadSessionContext(
    _3DProductDraft ProductDraft,
    TurboSquidNetworkCredential Credential,
    TurboSquidAwsUploadCredentials AwsUploadCredentials)
{
    internal Uri UploadEndpointFor(FileStream asset, string unixTimestamp) =>
        UploadEndpointFor(asset.Name, unixTimestamp);

    internal Uri UploadEndpointFor(string assetPath, string unixTimestamp) =>
        new(UploadEndpointPrefixWith(unixTimestamp), Path.GetFileName(assetPath));

    Uri UploadEndpointPrefixWith(string unixTimestamp) => new(UploadEndpointPrefix, unixTimestamp+'/');

    Uri UploadEndpointPrefix => _uploadEndpointPrefix ??=
        new($"https://{AwsUploadCredentials.Bucket}.s3.amazonaws.com/{AwsUploadCredentials.KeyPrefix}");
    Uri? _uploadEndpointPrefix;
}
