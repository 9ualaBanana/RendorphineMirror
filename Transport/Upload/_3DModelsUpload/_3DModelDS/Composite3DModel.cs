namespace Transport.Upload._3DModelsUpload._3DModelDS;

public record Composite3DModel : IDisposable
{
    public IEnumerable<_3DModel> _3DModels { get; }
    public IEnumerable<_3DModelPreviewImage> PreviewImages { get; }
    public _3DModelMetadata Metadata { get; }

    #region Initialization

    public static Composite3DModel FromDirectory(string directory, _3DModelMetadata metadata) => new(
        _3DModel._EnumerateIn(directory)
            .Select(_3DModelContainer => _3DModel.FromContainer(_3DModelContainer.OriginalPath)),
        _3DModelPreviewImage._EnumerateIn(directory),
        metadata);

    Composite3DModel(IEnumerable<_3DModel> _3DModels, IEnumerable<_3DModelPreviewImage> previews, _3DModelMetadata metadata)
    {
        PreviewImages = previews;
        this._3DModels = _3DModels;
        Metadata = metadata;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    { Dispose(true); GC.SuppressFinalize(this); }

    protected void Dispose(bool managed)
    {
        if (isDisposed) return;

        if (managed)
        {
            foreach (var _3DModel in _3DModels)
                _3DModel.Dispose();
        }

        isDisposed = true;
    }
    bool isDisposed;

    #endregion
}
