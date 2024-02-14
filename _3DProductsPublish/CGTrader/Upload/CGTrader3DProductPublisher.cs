using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.CGTrader._3DModelComponents;
using _3DProductsPublish.CGTrader.Api;
using _3DProductsPublish.CGTrader.Network;
using System.Net;

namespace _3DProductsPublish.CGTrader.Upload;

public class CGTrader3DProductPublisher
{
    readonly CGTraderApi _api;

    public CGTrader3DProductPublisher(CGTraderApi api)
    {
        _api = api;
    }

    public async Task PublishAsync(
        _3DProduct<CGTrader3DProductMetadata> _3DModel,
        NetworkCredential credential,
        CancellationToken cancellationToken)
    {
        var sessionContext = await CGTraderSessionContext._CreateAsyncUsing(_api, new(credential.UserName, credential.Password, true), cancellationToken);

        await _api._LoginAsync(sessionContext, cancellationToken);
        var modelDraft = await _api._CreateNewModelDraftAsyncFor(_3DModel, sessionContext, cancellationToken);
        await _api._UploadAssetsAsync(modelDraft, cancellationToken);
        await _api._UploadMetadataAsync(modelDraft, cancellationToken);
        await _api._PublishAsync(modelDraft, cancellationToken);
    }
}
