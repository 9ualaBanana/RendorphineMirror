namespace _3DProductsPublish.CGTrader;

public partial class CGTrader
{
    public partial record _3DProduct : _3DProductDS._3DProduct
    {
        public Metadata__ Metadata { get; }
        public Tracker_ Tracker { get; init; }

        public _3DProduct(_3DProductDS._3DProduct _3DProduct, Metadata__ metadata)
            : base(_3DProduct)
        {
            Metadata = metadata;
            Tracker = new(this);
        }
    }
}
