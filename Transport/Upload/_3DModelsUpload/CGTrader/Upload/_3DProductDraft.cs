using Transport.Upload._3DModelsUpload._3DModelDS;
using Transport.Upload._3DModelsUpload.CGTrader._3DModelComponents;
using Transport.Upload._3DModelsUpload.Turbosquid._3DModelComponents;

namespace Transport.Upload._3DModelsUpload.CGTrader.Upload;

internal record _3DProductDraft(_3DProduct _Product, string _ID)
{
    internal IEnumerable<T> _UpcastThumbnailsTo<T>() where T : _3DProductThumbnail
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
