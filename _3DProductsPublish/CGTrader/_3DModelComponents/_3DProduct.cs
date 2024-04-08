using _3DProductsPublish._3DProductDS;

namespace _3DProductsPublish.CGTrader._3DModelComponents;

public record _3DProduct<TMetadata> : _3DProduct
{
    internal _3DProduct(_3DProduct _3DProduct, TMetadata metadata)
        : base(_3DProduct)
    { Metadata = metadata; }
    public readonly TMetadata Metadata;
}
