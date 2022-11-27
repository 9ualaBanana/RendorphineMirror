using Transport.Upload._3DModelsUpload.CGTrader._3DModelComponents;

namespace Transport.Upload._3DModelsUpload.CGTrader.Upload;

internal record Composite3DModelDraft(Composite3DModel _Model, string _DraftID)
{
    internal IEnumerable<CGTrader3DModelPreviewImage> _UpcastPreviewImages => _Model.PreviewImages
        .Select(previewImage => new CGTrader3DModelPreviewImage(previewImage.FilePath));
}
