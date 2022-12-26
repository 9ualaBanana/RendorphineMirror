using System.Net;
using Transport.Upload._3DModelsUpload._3DModelDS;
using Transport.Upload._3DModelsUpload.Turbosquid.Api;
using Transport.Upload._3DModelsUpload.Turbosquid.Network.Authenticity;

namespace Transport.Upload._3DModelsUpload.Turbosquid.Upload;

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
