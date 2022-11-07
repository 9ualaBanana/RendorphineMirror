using Transport.Upload._3DModelsUpload.CGTrader.Models;
using Transport.Upload._3DModelsUpload.Models;

namespace Transport.Upload._3DModelsUpload.Services;

internal abstract class _3DModelUploaderBase<TMetadata> where TMetadata : _3DModelMetadata
{
    protected HttpClient HttpClient;


    protected _3DModelUploaderBase(HttpClient httpClient) => HttpClient = httpClient;


    internal abstract Task UploadAsync(
        CGTraderNetworkCredential credential,
        Composite3DModel composite3DModel,
        TMetadata metadata,
        CancellationToken cancellationToken = default);
}
