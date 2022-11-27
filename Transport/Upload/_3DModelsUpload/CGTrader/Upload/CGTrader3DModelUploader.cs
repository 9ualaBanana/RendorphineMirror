using Transport.Upload._3DModelsUpload.CGTrader._3DModelComponents;
using Transport.Upload._3DModelsUpload.CGTrader.Api;
using Transport.Upload._3DModelsUpload.CGTrader.Network;

namespace Transport.Upload._3DModelsUpload.CGTrader.Upload;

internal class CGTrader3DModelUploader : _3DModelUploaderBase
{
    readonly CGTraderApi _api;

    internal CGTrader3DModelUploader(HttpClient httpClient) : base(httpClient)
    {
        _api = new(httpClient);
    }

    internal override async Task UploadAsync(
        CGTraderNetworkCredential credential,
        Composite3DModel composite3DModel,
        CancellationToken cancellationToken = default)
    {
        var sessionContext = await CGTraderSessionContext.CreateAsyncUsing(_api, credential, cancellationToken);

        HttpClient.DefaultRequestHeaders._AddOrReplaceCsrfToken(sessionContext.CsrfToken);

        await _api._LoginAsync(sessionContext, cancellationToken);
        string modelDraftId = await _api._CreateNewModelDraftAsync(sessionContext, cancellationToken);
        await _api._UploadModelAssetsAsyncOf(composite3DModel, modelDraftId, cancellationToken);
        await _api._UploadModelMetadataAsync((composite3DModel.Metadata as CGTrader3DModelMetadata)!, modelDraftId, cancellationToken);
        await _api._PublishModelAsync(composite3DModel, modelDraftId, cancellationToken);
    }
}
