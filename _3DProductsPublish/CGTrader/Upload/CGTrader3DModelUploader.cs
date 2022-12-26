using _3DProductsPublish;
using _3DProductsPublish._3DModelDS;
using _3DProductsPublish.CGTrader.Api;
using _3DProductsPublish.CGTrader.Network;
using System.Net;

namespace _3DProductsPublish.CGTrader.Upload;

internal class CGTrader3DModelUploader : I3DProductUploader
{
    readonly CGTraderApi _api;

    internal CGTrader3DModelUploader()
    {
        _api = new(new HttpClient());
    }

    public async Task UploadAsync(
        _3DProduct _3DModel,
        NetworkCredential credential,
        CancellationToken cancellationToken)
    {
        var sessionContext = await CGTraderSessionContext._CreateAsyncUsing(_api, (credential as CGTraderNetworkCredential)!, cancellationToken);

        await _api._LoginAsync(sessionContext, cancellationToken);
        var modelDraft = await _api._CreateNewModelDraftAsyncFor(_3DModel, sessionContext, cancellationToken);
        await _api._UploadAssetsAsync(modelDraft, cancellationToken);
        await _api._UploadMetadataAsync(modelDraft, cancellationToken);
        await _api._PublishAsync(modelDraft, cancellationToken);
    }
}
