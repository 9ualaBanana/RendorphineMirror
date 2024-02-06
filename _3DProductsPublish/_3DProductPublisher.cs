using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid.Upload;
using Autofac;

namespace _3DProductsPublish;

public partial class _3DProductPublisher
{
    readonly TurboSquid3DProductPublisher _turboSquid;
    readonly INodeGui NodeGui;

    public static async Task<_3DProductPublisher> InitializeAsync(IComponentContext container, Credentials credentials, CancellationToken cancellationToken = default)
        => new(
            await TurboSquid3DProductPublisher.InitializeAsync(credentials.TurboSquid, container.Resolve<INodeGui>(), cancellationToken),
            credentials,
            container.Resolve<INodeGui>());

    _3DProductPublisher(TurboSquid3DProductPublisher turboSquid, Credentials credentials, INodeGui nodeGui)
    {
        _turboSquid = turboSquid;
        NodeGui = nodeGui;
    }

    public async Task PublishAsync(
        _3DProduct _3DProduct,
        _3DProduct.Metadata_ metadata,
        CancellationToken cancellationToken = default)
    {
        await _turboSquid.PublishAsync(await _3DProduct.AsyncWithTurboSquid(metadata, NodeGui, cancellationToken), cancellationToken);
    }
}
