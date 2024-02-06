using _3DProductsPublish._3DProductDS;
using System.Net;

namespace _3DProductsPublish.Turbosquid.Upload;

internal class TurboSquid3DProductPublisher
{
    readonly TurboSquid _turboSquid;

    internal static async Task<TurboSquid3DProductPublisher> InitializeAsync(NetworkCredential credential, INodeGui nodeGui, CancellationToken cancellationToken)
        => new(await TurboSquid.LogInAsyncUsing(credential, nodeGui, cancellationToken));

    TurboSquid3DProductPublisher(TurboSquid turboSquid)
    {
        _turboSquid = turboSquid;
    }

    public async Task PublishAsync(TurboSquid3DProduct _3DProduct, CancellationToken cancellationToken)
        => await _turboSquid.PublishAsync(_3DProduct, cancellationToken);
}
