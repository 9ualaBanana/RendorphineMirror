using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.CGTrader._3DModelComponents;

namespace _3DProductsPublish.CGTrader.Upload;

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
