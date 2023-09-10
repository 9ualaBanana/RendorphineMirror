using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.CGTrader.Upload;
using _3DProductsPublish.Turbosquid.Upload;

namespace _3DProductsPublish;

public partial class _3DProductPublisher
{
    readonly CGTrader3DProductPublisher _cgTrader;
    readonly TurboSquid3DProductPublisher _turboSquid;
    readonly Credentials _credentials;

    public static async Task<_3DProductPublisher> InitializeAsync(Credentials credentials, CancellationToken cancellationToken = default)
        => new(
            new CGTrader3DProductPublisher(),
            await TurboSquid3DProductPublisher.InitializeAsync(credentials.TurboSquid, cancellationToken),
            credentials);

    _3DProductPublisher(CGTrader3DProductPublisher cgTrader, TurboSquid3DProductPublisher turboSquid, Credentials credentials)
    {
        _cgTrader = cgTrader;
        _turboSquid = turboSquid;
        _credentials = credentials;
    }

    public async Task PublishAsync(
        _3DProduct _3DProduct,
        _3DProduct.Metadata_ metadata,
        CancellationToken cancellationToken = default)
    {
        await _cgTrader.PublishAsync(_3DProduct.WithCGTrader(metadata), _credentials.CGTrader, cancellationToken);
        await _turboSquid.PublishAsync(await _3DProduct.AsyncWithTurboSquid(metadata, cancellationToken), _credentials.TurboSquid, cancellationToken);
    }
}
