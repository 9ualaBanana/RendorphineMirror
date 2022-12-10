using System.Net;
using Transport.Upload._3DModelsUpload.CGTrader.Api;
using Transport.Upload._3DModelsUpload.CGTrader.Network;

namespace Transport.Upload._3DModelsUpload.CGTrader.Upload;

internal class CGTrader3DModelUploader : I3DModelUploader
{
    readonly CGTraderApi _api;

    internal CGTrader3DModelUploader()
    {
        _api = new(new HttpClient());
    }

    public async Task UploadAsync(
        NetworkCredential credential,
        Composite3DModel composite3DModel,
        CancellationToken cancellationToken)
    {
        var sessionContext = await CGTraderSessionContext._CreateAsyncUsing(_api, (credential as CGTraderNetworkCredential)!, cancellationToken);

        await _api._LoginAsync(sessionContext, cancellationToken);
        var modelDraft = await _api._CreateNewModelDraftAsyncFor(composite3DModel, sessionContext, cancellationToken);
        await _api._UploadAssetsAsync(modelDraft, cancellationToken);
        await _api._UploadMetadataAsync(modelDraft, cancellationToken);
        await _api._PublishAsync(modelDraft, cancellationToken);
    }
}
