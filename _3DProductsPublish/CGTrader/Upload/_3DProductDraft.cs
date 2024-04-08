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

    internal IEnumerable<CGTrader3DModelThumbnail> UpcastThumbnails()
        =>  Product.Thumbnails.Cast<CGTrader3DModelThumbnail>();
}
