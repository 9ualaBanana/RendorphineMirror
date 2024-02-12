using static _3DProductsPublish._3DProductDS._3DProduct;

namespace _3DProductsPublish._3DProductDS;

/// <summary>
/// Wraps either directory or archive in which 3D model parts are stored.
/// </summary>
public partial record _3DModel : AssetContainer, I3DProductAsset, IDisposable
{
    #region Initialization

    public static _3DModel FromContainer(string path, bool disposeTemps = true) => new(path, disposeTemps);

    _3DModel(string containerPath, bool disposeTemps = true)
        : base(containerPath, disposeTemps)
    {
    }

    #endregion


    internal static IEnumerable<_3DModel> EnumerateAt(string directoryPath, bool disposeTemps = true)
    {
        var _3DModelContainers = AssetContainer.EnumerateAt(directoryPath).ToList();
        // TODO: _3DModel shouldn't know about Textures_ of _3DProduct.
        _3DModelContainers.Remove(System.IO.Path.Combine(directoryPath, Textures_.ContainerName));

        return _3DModelContainers.Select(containerPath => new _3DModel(containerPath, disposeTemps));
    }


    public static implicit operator _3DModel(string containerPath) =>
        new(containerPath);


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
