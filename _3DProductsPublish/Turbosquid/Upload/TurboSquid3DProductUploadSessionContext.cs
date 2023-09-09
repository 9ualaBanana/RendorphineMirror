using _3DProductsPublish.CGTrader.Upload;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Network.Authenticity;

namespace _3DProductsPublish.Turbosquid.Upload;

internal record TurboSquid3DProductUploadSessionContext(
    _3DProductDraft<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata> ProductDraft,
    TurboSquidNetworkCredential Credential,
    TurboSquidAwsUploadCredentials AwsUploadCredentials,
    HttpClient HttpClient)
{
    internal Uri UploadEndpointFor(FileStream asset, string unixTimestamp) =>
        new(new Uri(new($"https://{AwsUploadCredentials.Bucket}.s3.amazonaws.com/{AwsUploadCredentials.KeyPrefix}"), unixTimestamp + '/'), Path.GetFileName(asset.Name));
}
