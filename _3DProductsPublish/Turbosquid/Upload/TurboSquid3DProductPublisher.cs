using _3DProductsPublish._3DProductDS;
using MarkTM.RFProduct;
using System.Net;

namespace _3DProductsPublish.Turbosquid.Upload;

public class TurboSquid3DProductPublisher
{
    readonly TurboSquid _turboSquid;
    readonly INodeGui _gui; 

    public static async Task<TurboSquid3DProductPublisher> InitializeAsync(NetworkCredential credential, INodeGui nodeGui, CancellationToken cancellationToken)
        => new(await TurboSquid.LogInAsyncUsing(credential, nodeGui, cancellationToken), nodeGui);

    TurboSquid3DProductPublisher(TurboSquid turboSquid, INodeGui gui)
    {
        _turboSquid = turboSquid;
        _gui = gui;
    }

    public async Task PublishAsync(RFProduct._3D rfProduct, CancellationToken cancellationToken)
    {
        await PublishAsync(await ConvertAsync(rfProduct, cancellationToken), cancellationToken);

        async Task<TurboSquid3DProduct> ConvertAsync(RFProduct._3D rfProduct, CancellationToken cancellationToken)
        {
            var idea = (RFProduct._3D.Idea_)rfProduct.Idea;
            var metadata = JObject.Parse(File.ReadAllText(idea.Metadata)).ToObject<_3DProduct.Metadata_>()!;
            return await _3DProduct.FromDirectory(rfProduct.Path).AsyncWithTurboSquid(metadata, _gui, cancellationToken);
        }
    }
    public async Task PublishAsync(TurboSquid3DProduct _3DProduct, CancellationToken cancellationToken)
        => await _turboSquid.PublishAsync(_3DProduct, cancellationToken);
}
