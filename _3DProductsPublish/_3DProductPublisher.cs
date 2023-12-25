﻿using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.CGTrader.Upload;
using _3DProductsPublish.Turbosquid.Upload;
using Autofac;

namespace _3DProductsPublish;

public partial class _3DProductPublisher
{
    readonly CGTrader3DProductPublisher _cgTrader;
    readonly TurboSquid3DProductPublisher _turboSquid;
    readonly Credentials _credentials;
    readonly INodeGui NodeGui;

    public static async Task<_3DProductPublisher> InitializeAsync(IComponentContext container, Credentials credentials, CancellationToken cancellationToken = default)
        => new(
            container.Resolve<CGTrader3DProductPublisher>(),
            await TurboSquid3DProductPublisher.InitializeAsync(credentials.TurboSquid, container.Resolve<INodeGui>(), cancellationToken),
            credentials,
            container.Resolve<INodeGui>());

    _3DProductPublisher(CGTrader3DProductPublisher cgTrader, TurboSquid3DProductPublisher turboSquid, Credentials credentials, INodeGui nodeGui)
    {
        _cgTrader = cgTrader;
        _turboSquid = turboSquid;
        _credentials = credentials;
        NodeGui = nodeGui;
    }

    public async Task PublishAsync(
        _3DProduct _3DProduct,
        _3DProduct.Metadata_ metadata,
        CancellationToken cancellationToken = default)
    {
        await _cgTrader.PublishAsync(_3DProduct.WithCGTrader(metadata), _credentials.CGTrader, cancellationToken);
        await _turboSquid.PublishAsync(await _3DProduct.AsyncWithTurboSquid(metadata, NodeGui, cancellationToken), _credentials.TurboSquid, cancellationToken);
    }
}
