using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.CGTrader._3DModelComponents;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Upload;
using _3DProductsPublish.Turbosquid.Upload.Processing;

namespace _3DProductsPublish.CGTrader.Upload;

// TODO: Implement the ChangeTracker ?
// TODO: Move PublishSession here
public record _3DProductDraft<TMetadata>
{
    public _3DProduct<TMetadata> Product { get; }
    public string ID { get; }

    public _3DProductDraft(_3DProduct<TMetadata> product, string id)
    {
        Product = product;
        ID = id;
    }

    internal IEnumerable<T> UpcastThumbnailsTo<T>() where T : _3DProductThumbnail
    {
        Func<_3DProductThumbnail, T> upcaster = typeof(T) switch
        {
            Type type
            when type == typeof(CGTrader3DModelThumbnail) =>
                thumbnail => (new CGTrader3DModelThumbnail(thumbnail.FilePath) as T)!,
            { } => thumbnail => (thumbnail as T)!
        };
        return Product.Thumbnails.Select(upcaster);
    }
}

public record _3DProductDraft<TProductMetadata, TModelMetadata>
    : _3DProductDraft<TProductMetadata>
    where TModelMetadata : _3DModel.IMetadata
{
    new public _3DProduct<TProductMetadata, TModelMetadata> Product;

    public _3DProductDraft(_3DProduct<TProductMetadata, TModelMetadata> product, int id)
    : this(product, id.ToString())
    {
    }
    internal _3DProductDraft(_3DProduct<TProductMetadata, TModelMetadata> product, string id)
        : base(product, id)
    {
        Product = product;
    }
}

internal record TurboSquid3DProductDraft
{
    internal long ID { get; init; }
    internal TurboSquidAwsSession AWS { get; init; }
    internal TurboSquid3DProduct LocalProduct { get; init; }
    internal TurboSquid3DProductMetadata.Product RemoteProduct { get; init; }

    public TurboSquid3DProductDraft(long id, TurboSquidAwsSession awsSession, TurboSquid3DProduct localProduct, TurboSquid3DProductMetadata.Product remoteProduct)
    {
        ID = id;
        AWS = awsSession;
        RemoteProduct = remoteProduct;
        LocalProduct = remoteProduct is null ? localProduct : localProduct.SynchronizedWith(remoteProduct);
    }

    internal IEnumerable<TurboSquidProcessed3DModel> Edited3DModels
        => LocalProduct._3DModels.Cast<TurboSquidProcessed3DModel>()
        .Join(RemoteProduct.files.Where(_ => _.type == "product_file"),
            local => local.FileId,
            remote => remote.id,
            (local, remote) => new { Local = local, Remote = remote })
        .Where(_ => !_.Remote.Equals(_.Local))
        .Select(_ => _.Local);
}
