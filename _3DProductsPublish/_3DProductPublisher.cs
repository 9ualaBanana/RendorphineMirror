using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.CGTrader.Upload;
using _3DProductsPublish.Turbosquid.Upload;
using System.Net;

namespace _3DProductsPublish;

public class _3DProductPublisher
{
    readonly CGTrader3DProductPublisher _cgTrader;
    readonly TurboSquid3DProductPublisher _turboSquid;

    public static async Task<_3DProductPublisher> InitializeAsync(NetworkCredential credential, CancellationToken cancellationToken = default)
        => new(
            new CGTrader3DProductPublisher(),
            await TurboSquid3DProductPublisher.InitializeAsync(credential, cancellationToken)
            );

    _3DProductPublisher(CGTrader3DProductPublisher cgTrader, TurboSquid3DProductPublisher turboSquid)
    {
        _cgTrader = cgTrader;
        _turboSquid = turboSquid;
    }

    public async Task PublishAsync(
        _3DProduct _3DProduct,
        _3DProduct.Metadata_ metadata,
        NetworkCredential credential,
        CancellationToken cancellationToken = default)
    {
        await _cgTrader.PublishAsync(_3DProduct.WithCGTrader(metadata), credential, cancellationToken);
        await _turboSquid.PublishAsync(await _3DProduct.AsyncWithTurboSquid(metadata, cancellationToken), credential, cancellationToken);
    }
}
