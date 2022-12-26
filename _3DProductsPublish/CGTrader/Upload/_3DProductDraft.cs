using _3DProductsPublish._3DModelDS;
using _3DProductsPublish.CGTrader._3DModelComponents;
using _3DProductsPublish.Turbosquid._3DModelComponents;

namespace _3DProductsPublish.CGTrader.Upload;

internal record _3DProductDraft(_3DProduct _Product, string _ID)
{
    internal IEnumerable<T> UpcastThumbnailsTo<T>() where T : _3DProductThumbnail
    {
        Func<_3DProductThumbnail, T> upcaster = typeof(T) switch
        {
            Type type
            when type == typeof(CGTrader3DModelThumbnail) =>
                thumbnail => (new CGTrader3DModelThumbnail(thumbnail.FilePath) as T)!,
            Type type
            when type == typeof(TurboSquid3DProductThumbnail) =>
                thumbnail => (new TurboSquid3DProductThumbnail(thumbnail.FilePath) as T)!,
            { } => thumbnail => (thumbnail as T)!
        };
        return _Product.Thumbnails.Select(upcaster);
    }
}
