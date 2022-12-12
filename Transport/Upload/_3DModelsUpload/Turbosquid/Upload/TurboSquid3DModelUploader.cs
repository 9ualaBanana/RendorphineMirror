using System.Net;
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
        await _api._LoginAsync(credential_, cancellationToken);
        var uploadSessionData = await _api._RequestModelUploadSessionDataAsyncFor(composite3DModel, cancellationToken);
    }
}
