using System.Net;
using Transport.Upload._3DModelsUpload._3DModelDS;
using Transport.Upload._3DModelsUpload.Turbosquid.Api;
using Transport.Upload._3DModelsUpload.Turbosquid.Network.Authenticity;

namespace Transport.Upload._3DModelsUpload.Turbosquid.Upload;

internal class TurboSquid3DModelUploader : I3DModelUploader
{
    readonly TurboSquidApi _api;

    internal TurboSquid3DModelUploader()
    {
        _api = new();
    }

    public async Task UploadAsync(
        Composite3DModel composite3DModel,
        NetworkCredential credential,
        CancellationToken cancellationToken)
    {
        var credential_ = await TurboSquidNetworkCredential._RequestAsyncUsing(_api, credential, cancellationToken);
        await _api._LoginAsyncUsing(credential_, cancellationToken);
        var uploadSessionContext = await _api._RequestModelUploadSessionContextAsyncFor(composite3DModel, cancellationToken);
        await _api._UploadAssetsAsync(uploadSessionContext, cancellationToken);
    }
}
