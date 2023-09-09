using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Api;
using System.Net;

namespace _3DProductsPublish.Turbosquid.Upload;

internal class TurboSquid3DProductPublisher : I3DProductPublisher<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata>
{
    readonly TurboSquidApi _api;

    internal TurboSquid3DProductPublisher()
    {
        _api = new();
    }

    public async Task PublishAsync(
        _3DProduct<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata> _3DProduct,
        NetworkCredential credential,
        CancellationToken cancellationToken)
    {
        var credential_ = await _api._RequestTurboSquidNetworkCredentialAsync(credential, cancellationToken);
        await _api.LoginAsyncUsing(credential_, cancellationToken);
        var context = await _api.RequestProductUploadSessionContextAsyncFor(_3DProduct, credential_, cancellationToken);
        await new TurboSquid3DProductCorePublisher(context).PublishProductAsync(cancellationToken);
    }
}
