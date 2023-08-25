using _3DProductsPublish._3DProductDS;
using System.Net;

namespace _3DProductsPublish;

internal interface I3DProductPublisher<TMetadata>
{
    Task PublishAsync(
        _3DProduct<TMetadata> _3DProduct,
        NetworkCredential credential,
        CancellationToken cancellationToken);
}
