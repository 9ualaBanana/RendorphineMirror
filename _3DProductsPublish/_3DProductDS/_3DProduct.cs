namespace _3DProductsPublish._3DProductDS;

public partial record _3DProduct : IDisposable
{
    /// <summary>
    /// Path to the directory from which this <see cref="_3DProduct"/> instance was initialized.
    /// </summary>
    public readonly string ContainerPath;
    public IEnumerable<_3DModel> _3DModels { get; }
    public IEnumerable<_3DProductThumbnail> Thumbnails { get; }
    public Textures_? Textures { get; }

    #region Initialization

    public static _3DProduct FromDirectory(string directoryPath) => new(
        directoryPath,
        _3DModel.EnumerateAt(directoryPath),
        _3DProductThumbnail.EnumerateAt(directoryPath),
        Textures_.FindAt(directoryPath));

    _3DProduct(
        string containerPath,
        IEnumerable<_3DModel> _3DModels,
        IEnumerable<_3DProductThumbnail> thumbnails,
        Textures_? textures = null)
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
    internal _3DProduct(_3DProduct _3DProduct, TMetadata metadata)
        : base(_3DProduct)
    { Metadata = metadata; }
    public readonly TMetadata Metadata;
}

public record _3DProduct<TProductMetadata, TModelsMetadata> : _3DProduct<TProductMetadata>
    where TModelsMetadata : I3DModelMetadata
{
    new public IEnumerable<_3DModel<TModelsMetadata>> _3DModels { get; }
    public int ID { get; internal set; } = default;

    internal _3DProduct(_3DProduct<TProductMetadata> _3DProduct, IEnumerable<TModelsMetadata> modelsMetadata)
        : base(_3DProduct)
    {
        _3DModels = _3DProduct._3DModels.OrderBy(_ => _.Name)
            .Zip(modelsMetadata.OrderBy(_ => _.Name))
            .Select(_ => new _3DModel<TModelsMetadata>(_.First, _.Second));
    }
}
