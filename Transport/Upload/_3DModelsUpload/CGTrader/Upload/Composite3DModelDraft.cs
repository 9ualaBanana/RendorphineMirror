using Transport.Upload._3DModelsUpload._3DModelDS;
using Transport.Upload._3DModelsUpload.CGTrader._3DModelComponents;
using Transport.Upload._3DModelsUpload.Turbosquid._3DModelComponents;

namespace Transport.Upload._3DModelsUpload.CGTrader.Upload;

internal record Composite3DModelDraft(Composite3DModel _Model, string _DraftID)
{
    internal IEnumerable<T> _UpcastPreviewImagesTo<T>() where T : _3DModelPreviewImage
    {
        Func<_3DModelPreviewImage, T> upcaster = typeof(T) switch
        {
            Type type
            when type == typeof(CGTrader3DModelPreviewImage) =>
                previewImage => (new CGTrader3DModelPreviewImage(previewImage.FilePath) as T)!,
            Type type
            when type == typeof(TurboSquid3DModelPreviewImage) =>
                previewImage => (new TurboSquid3DModelPreviewImage(previewImage.FilePath) as T)!,
            { } => previewImage => (previewImage as T)!
        };
        return _Model.PreviewImages.Select(upcaster);
    }
}
