using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Api;
using _3DProductsPublish.Turbosquid.Network.Authenticity;
using System.Net;

namespace _3DProductsPublish.Turbosquid.Upload;

internal class TurboSquid3DProductPublisher : I3DProductPublisher<TurboSquid3DProductMetadata>
{
    readonly TurboSquidApi _api;

    internal TurboSquid3DProductPublisher()
    {
        _api = new();
    }

    public async Task PublishAsync(
        _3DProduct<TurboSquid3DProductMetadata> _3DProduct,
        NetworkCredential credential,
        CancellationToken cancellationToken)
    {
        var credential_ = await TurboSquidNetworkCredential._RequestAsyncUsing(_api, credential, cancellationToken);
        await _api._LoginAsyncUsing(credential_, cancellationToken);
        var productUploadSessionContext = await _api.RequestProductUploadSessionContextAsyncFor(_3DProduct, credential_, cancellationToken);
        await _api.PublishAsyncUsing(productUploadSessionContext, cancellationToken);
    }
}
