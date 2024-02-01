using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.CGTrader._3DModelComponents;

namespace _3DProductsPublish.CGTrader.Upload;

// _3DProductDraft is actually what I'm looking for to represent a product for which the draft is created on the turbosquid servers.
// TODO: Implement the ChangeTracker.
// TODO: Draft existence on turbosquid servers should be checked by requesting edit on draft with the corresponding ID.
// TODO: If ID is unknown, then the draft must be created first.
// The draft stored on turbosquid servers shall be requested and compared to the local _3DProductDraft object and all local changes shall be communicated to the draft on turbosquid servers and it shall be published.
// TODO: Move PublishSession here.
// TODO: Inherit from _3DProduct.
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

public record _3DProductDraft<TProductMetadata, TModelsMetadata>
    : _3DProductDraft<TProductMetadata>
    where TModelsMetadata : I3DModelMetadata
{
    new public _3DProduct<TProductMetadata, TModelsMetadata> Product;

    internal _3DProductDraft(_3DProduct<TProductMetadata, TModelsMetadata> product, string id)
        : base(product, id)
    {
        Product = product;
    }
}
