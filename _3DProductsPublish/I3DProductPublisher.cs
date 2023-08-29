using _3DProductsPublish._3DProductDS;
using System.Net;

namespace _3DProductsPublish;

internal interface I3DProductPublisher<TProductMetadata>
{
    Task PublishAsync(
        _3DProduct<TProductMetadata> _3DProduct,
        NetworkCredential credential,
        CancellationToken cancellationToken);
}

internal interface I3DProductPublisher<TProductMetadata, TModelsMetadata>
    where TModelsMetadata : I3DModelMetadata
{
    Task PublishAsync(
        _3DProduct<TProductMetadata, TModelsMetadata> _3DProduct,
        NetworkCredential credential,
        CancellationToken cancellationToken);
}
