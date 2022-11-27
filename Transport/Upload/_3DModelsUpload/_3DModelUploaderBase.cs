using Transport.Upload._3DModelsUpload.CGTrader.Network;

namespace Transport.Upload._3DModelsUpload;

internal abstract class _3DModelUploaderBase
{
    protected HttpClient HttpClient;


    protected _3DModelUploaderBase(HttpClient httpClient) => HttpClient = httpClient;


    internal abstract Task UploadAsync(
        CGTraderNetworkCredential credential,
        Composite3DModel composite3DModel,
        CancellationToken cancellationToken = default);
}
