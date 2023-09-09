using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.CGTrader._3DModelComponents;

namespace _3DProductsPublish.CGTrader.Upload;

internal record _3DProductDraft<TMetadata>(_3DProduct<TMetadata> _Product, string _ID)
{
    internal IEnumerable<T> UpcastThumbnailsTo<T>() where T : _3DProductThumbnail
    {
        Func<_3DProductThumbnail, T> upcaster = typeof(T) switch
        {
            Type type
            when type == typeof(CGTrader3DModelThumbnail) =>
                thumbnail => (new CGTrader3DModelThumbnail(thumbnail.FilePath) as T)!,
            { } => thumbnail => (thumbnail as T)!
        };
        return _Product.Thumbnails.Select(upcaster);
    }
}

internal record _3DProductDraft<TProductMetadata, TModelsMetadata>
    : _3DProductDraft<TProductMetadata>
    where TModelsMetadata : I3DModelMetadata
{
    new internal _3DProduct<TProductMetadata, TModelsMetadata> _Product;

    internal _3DProductDraft(_3DProduct<TProductMetadata, TModelsMetadata> product, string id)
        : base(product, id)
    {
        _Product = product;
    }
}
