namespace _3DProductsPublish._3DProductDS;

/// <summary>
/// Wraps either directory or archive in which 3D model parts are stored.
/// </summary>
public partial record _3DModel : _3DProduct.AssetContainer, I3DProductAsset, IDisposable
{
    public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);

    #region Initialization

    public static _3DModel FromContainer(string path, bool disposeTemps = true) => new(path, disposeTemps);

    _3DModel(string containerPath, bool disposeTemps = true)
        : base(containerPath, disposeTemps)
    {
    }

    #endregion


    internal static IEnumerable<_3DModel> EnumerateAt(string directoryPath, bool disposeTemps = true)
    {
        var _3DModelContainers = _3DProduct.AssetContainer.EnumerateAt(directoryPath).ToList();

        return _3DModelContainers.Select(containerPath => new _3DModel(containerPath, disposeTemps));
    }


    public static implicit operator _3DModel(string containerPath) =>
        new(containerPath);
}

public partial record _3DModel<TMetadata> : _3DModel
    where TMetadata : I3DModelMetadata
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

public interface I3DModelMetadata
{
    string Name { get; }
}

static class _3DModelFilesExtensions
{
    internal static void CopyTo(this IEnumerable<string> files, DirectoryInfo directory)
    {
        foreach (string filePath in files)
        {
            string destinationFilePath = Path.Combine(directory.FullName, Path.GetFileName(filePath));
            File.Copy(filePath, destinationFilePath);
        }
    }
}
