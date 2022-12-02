using System.Net;

namespace Transport.Upload._3DModelsUpload;

internal abstract class _3DModelUploaderBase
{
    protected HttpClient HttpClient;


    protected _3DModelUploaderBase(HttpClient httpClient) => HttpClient = httpClient;


    internal abstract Task UploadAsync(
        NetworkCredential credential,
        Composite3DModel composite3DModel,
        CancellationToken cancellationToken = default);
}
