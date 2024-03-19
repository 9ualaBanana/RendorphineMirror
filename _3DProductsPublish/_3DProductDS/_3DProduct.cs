namespace _3DProductsPublish._3DProductDS;

public partial record _3DProduct : IDisposable
{
    /// <summary>
    /// Path to the directory from which this <see cref="_3DProduct"/> instance was initialized.
    /// </summary>
    public readonly string ContainerPath;
    public List<_3DModel> _3DModels { get; }
    public List<_3DProductThumbnail> Thumbnails { get; internal set; }
    public List<Textures_> Textures { get; }

    #region Initialization

    public static _3DProduct FromDirectory(string directoryPath) => new(
        directoryPath,
        _3DModel.EnumerateAt(directoryPath).ToList(),
        _3DProductThumbnail.EnumerateAt(directoryPath).ToList(),
        Textures_.EnumerateAt(directoryPath).ToList());

    internal _3DProduct(string containerPath,
        List<_3DModel> _3DModels,
        List<_3DProductThumbnail> thumbnails,
        List<Textures_> textures)
    {
        ContainerPath = containerPath;
        this._3DModels = _3DModels;
        Thumbnails = thumbnails;
        Textures = textures;
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

public record _3DProduct<TMetadata> : _3DProduct
{
    public long ID { get; internal set; } = default;
    internal _3DProduct(_3DProduct _3DProduct, TMetadata metadata)
        : base(_3DProduct)
    { Metadata = metadata; }
    public readonly TMetadata Metadata;
}

public record _3DProduct<TProductMetadata, TModelsMetadata> : _3DProduct<TProductMetadata>
    where TModelsMetadata : _3DModel.IMetadata
{
    new public List<_3DModel<TModelsMetadata>> _3DModels { get; }

    internal _3DProduct(_3DProduct<TProductMetadata> _3DProduct, IEnumerable<TModelsMetadata> modelsMetadata)
        : base(_3DProduct)
    {
        _3DModels = _3DProduct._3DModels.Join(modelsMetadata,
            _3DModel => _3DModel.Name(),
            metadata => metadata.Name,  // IMetadata.Name is used here.
            (_3DModel, metadata) => new _3DModel<TModelsMetadata>(_3DModel, metadata))
            .ToList();
    }
}
