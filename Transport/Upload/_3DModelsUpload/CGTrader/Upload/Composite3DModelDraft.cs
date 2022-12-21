using Transport.Upload._3DModelsUpload._3DModelDS;
using Transport.Upload._3DModelsUpload.CGTrader._3DModelComponents;
using Transport.Upload._3DModelsUpload.Turbosquid._3DModelComponents;

namespace Transport.Upload._3DModelsUpload.CGTrader.Upload;

internal record Composite3DModelDraft(Composite3DModel _Model, string _DraftID)
{
    internal IEnumerable<T> _UpcastThumnailsTo<T>() where T : _3DModelThumbnail
    {
        Func<_3DModelThumbnail, T> upcaster = typeof(T) switch
        {
            Type type
            when type == typeof(CGTrader3DModelThumbnail) =>
                thumbnail => (new CGTrader3DModelThumbnail(thumbnail.FilePath) as T)!,
            Type type
            when type == typeof(TurboSquid3DModelThumbnail) =>
                thumbnail => (new TurboSquid3DModelThumbnail(thumbnail.FilePath) as T)!,
            { } => thumbnail => (thumbnail as T)!
        };
        return _Model.Thumbnails.Select(upcaster);
    }
}
