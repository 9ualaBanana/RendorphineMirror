namespace _3DProductsPublish._3DModelDS;

public record _3DProduct : IDisposable
{
    public IEnumerable<_3DModel> _3DModels { get; }
    public IEnumerable<_3DProductThumbnail> Thumbnails { get; }
    public _3DModelMetadata Metadata { get; }

    #region Initialization

    public static _3DProduct FromDirectory(string directory, _3DModelMetadata metadata) => new(
        _3DModel._EnumerateIn(directory)
            .Select(_3DModelContainer => _3DModel.FromContainer(_3DModelContainer.OriginalPath)),
        _3DProductThumbnail._EnumerateIn(directory),
        metadata);

    _3DProduct(IEnumerable<_3DModel> _3DModels, IEnumerable<_3DProductThumbnail> thumbnails, _3DModelMetadata metadata)
    {
        this._3DModels = _3DModels;
        Thumbnails = thumbnails;
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
