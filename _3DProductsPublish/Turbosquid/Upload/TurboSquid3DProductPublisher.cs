using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using System.Net;

namespace _3DProductsPublish.Turbosquid.Upload;

internal class TurboSquid3DProductPublisher : I3DProductPublisher<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata>
{
    readonly TurboSquid _turboSquid;

    internal static async Task<TurboSquid3DProductPublisher> InitializeAsync(NetworkCredential credential, INodeGui nodeGui, CancellationToken cancellationToken)
        => new(await TurboSquid.LogInAsyncUsing(credential, nodeGui, cancellationToken));

    TurboSquid3DProductPublisher(TurboSquid turboSquid)
    {
        _turboSquid = turboSquid;
    }

    public async Task PublishAsync(
        _3DProduct<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata> _3DProduct,
        NetworkCredential credential,
        CancellationToken cancellationToken)
        => await _turboSquid.PublishAsync(_3DProduct, cancellationToken);
}
