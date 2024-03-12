using _3DProductsPublish.Turbosquid._3DModelComponents;
using MarkTM.RFProduct;

namespace _3DProductsPublish._3DProductDS;

public partial record _3DModel(string Path) : I3DProductAsset, IDisposable
{
    public static implicit operator string(_3DModel model) => model.Path;

    // _3DModel shall be initialized as archived.
    internal string Archived => _archived ??= RFProduct._3D.Idea_.IsPackage(this) ? this : AssetContainer.Archive_.Pack(this);
    string? _archived;


    internal static IEnumerable<_3DModel> EnumerateAt(string directoryPath)
        => Directory.EnumerateFiles(directoryPath).Where(FileFormat_.IsKnown)
        .Select(_ => new _3DModel(_));

    #region IDisposable

    public void Dispose()
    { Dispose(true); GC.SuppressFinalize(this); }

    protected void Dispose(bool managed)
    {
        if (managed)
        {
            if (!_isDisposed)
            {
                if (_archived is not null)
                    File.Delete(_archived);

                _isDisposed = true;
            }
        }
    }
    bool _isDisposed;

    #endregion

    public interface IMetadata { string Name { get; } }
}

public partial record _3DModel<TMetadata> : _3DModel
    where TMetadata : _3DModel.IMetadata
{
    internal readonly TMetadata Metadata;

    internal _3DModel(_3DModel _3DModel, TMetadata metadata)
        : base(_3DModel)
    {
        Metadata = metadata;
    }

    protected _3DModel(_3DModel<TMetadata> _3DModel)
        : base(_3DModel)
    {
        Metadata = _3DModel.Metadata;
    }
}
