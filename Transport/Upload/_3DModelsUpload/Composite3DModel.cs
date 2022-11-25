namespace Transport.Upload._3DModelsUpload;

public record Composite3DModel : IDisposable
{
    public IEnumerable<_3DModelPreviewImage> PreviewImages { get; internal set; }
    public IEnumerable<_3DModel> _3DModels { get; }
    public _3DModelMetadata Metadata { get; }

    #region Initialization

    public static Composite3DModel FromDirectory(string directory, _3DModelMetadata metadata) => new(
        _3DModelPreviewImage._EnumerateIn(directory),
        _3DModel._EnumerateIn(directory)
            .Select(_3DModelContainer => _3DModel.FromContainer(_3DModelContainer.OriginalPath)),
        metadata
        );

    Composite3DModel(IEnumerable<_3DModelPreviewImage> previews, IEnumerable<_3DModel> _3DModels, _3DModelMetadata metadata)
    {
        PreviewImages = previews;
        this._3DModels = _3DModels;
        Metadata = metadata;
    }

    #endregion

    /// <remarks>
    /// Should only be called from _3DModelUploader as a preliminary to the actual upload.
    /// </remarks>
    internal void Archive() { foreach (var _3DModel in _3DModels) _3DModel.Archive(); }

    #region IDisposable

    ~Composite3DModel() => Dispose(false);

    public void Dispose()
    { Dispose(true); GC.SuppressFinalize(this); }

    protected void Dispose(bool _)
    {
        if (isDisposed) return;

        foreach (var _3DModel in _3DModels)
            _3DModel.Dispose();

        isDisposed = true;
    }
    bool isDisposed;

    #endregion
}
