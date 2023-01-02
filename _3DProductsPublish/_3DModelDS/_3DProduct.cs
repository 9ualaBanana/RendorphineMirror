namespace _3DProductsPublish._3DModelDS;

public record _3DProduct : IDisposable
{
    /// <summary>
    /// Path to the directory from which this <see cref="_3DProduct"/> instance was initialized.
    /// </summary>
    public readonly string ContainerPath;
    public IEnumerable<_3DModel> _3DModels { get; }
    public IEnumerable<_3DProductThumbnail> Thumbnails { get; }
    public _3DModelMetadata Metadata { get; }

    #region Initialization

    public static _3DProduct FromDirectory(string directoryPath, _3DModelMetadata metadata) => new(
        directoryPath,
        _3DModel.EnumerateIn(directoryPath)
            .Select(_3DModelContainer => _3DModel.FromContainer(_3DModelContainer.OriginalPath)),
        _3DProductThumbnail.EnumerateIn(directoryPath),
        metadata);

    _3DProduct(string containerPath, IEnumerable<_3DModel> _3DModels, IEnumerable<_3DProductThumbnail> thumbnails, _3DModelMetadata metadata)
    {
        ContainerPath = containerPath;
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
        if (managed)
        {
            if (!_isDisposed)
            {
                foreach (var _3DModel in _3DModels)
                    _3DModel.Dispose();

                _isDisposed = true;
            }
        }
    }
    bool _isDisposed;

    #endregion
}
