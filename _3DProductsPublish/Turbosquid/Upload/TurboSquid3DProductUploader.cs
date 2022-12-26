using _3DProductsPublish;
using _3DProductsPublish._3DModelDS;
using _3DProductsPublish.Turbosquid.Api;
using _3DProductsPublish.Turbosquid.Network.Authenticity;
using System.Net;

namespace _3DProductsPublish.Turbosquid.Upload;

internal class TurboSquid3DProductUploader : I3DProductUploader
{
    readonly TurboSquidApi _api;

    internal TurboSquid3DProductUploader()
    {
        _api = new();
    }

    public async Task UploadAsync(
        _3DProduct _3DProduct,
        NetworkCredential credential,
        CancellationToken cancellationToken)
    {
        var credential_ = await TurboSquidNetworkCredential._RequestAsyncUsing(_api, credential, cancellationToken);
        await _api._LoginAsyncUsing(credential_, cancellationToken);
        var productUploadSessionContext = await _api._RequestProductUploadSessionContextAsyncFor(_3DProduct, credential_, cancellationToken);
        await _api._UploadAssetsAsyncUsing(productUploadSessionContext, cancellationToken);
    }
}
