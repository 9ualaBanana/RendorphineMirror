namespace _3DProductsPublish._3DProductDS;

/// <summary>
/// Wraps either directory or archive in which 3D model parts are stored.
/// </summary>
public partial class _3DModel : I3DProductAsset, IDisposable
{
    internal readonly ContainerType OriginalContainer;

    /// <summary>
    /// Refers to the path from which this <see cref="_3DModel"/> was initialized.
    /// </summary>
    public readonly string OriginalPath;
    string? _directoryPath;
    string? _archivePath;

    #region ContentManagement

    /// <summary>
    /// Creates a temporary archive to which the files are extracted if the <see cref="_3DModel"/>
    /// is initialized from a directory (i.e. <see cref="OriginalPath"/> referes to a directory).
    /// </summary>
    /// <returns>Path to the archive where this <see cref="_3DModel"/> is stored.</returns>
    internal async ValueTask<string> ArchiveAsync(CancellationToken cancellationToken = default)
        => _archivePath ??= await _3DModelArchiver.ArchiveAsync(this, cancellationToken);

    /// <remarks>
    /// Creates a temporary directory to which the files are extracted if the <see cref="_3DModel"/>
    /// is initialized from an archive (i.e. <see cref="OriginalPath"/> refers an archive).
    /// </remarks>
    /// <returns>Paths of files that make up this <see cref="_3DModel"/>.</returns>
    public IEnumerable<string> Files
        => Directory.EnumerateFiles(_directoryPath ??= _3DModelArchiver.Unpack(this));

    #endregion

    #region Initialization

    public static _3DModel FromContainer(string path, bool disposeTemps = true) => new(path, disposeTemps);

    _3DModel(string containerPath, bool disposeTemps = true)
    {
        if (Directory.Exists(containerPath))
        { OriginalPath = _directoryPath = containerPath; OriginalContainer = ContainerType.Directory; }
        else if (Archive.Exists(containerPath))
        { OriginalPath = _archivePath = containerPath; OriginalContainer = ContainerType.Archive; }
        else throw new ArgumentException("Container must reference either directory or archive.", nameof(containerPath));

        _disposeTemps = disposeTemps;
    }

    protected _3DModel(_3DModel prototype)
    {
        switch (prototype.OriginalContainer)
        {
            case ContainerType.Directory:
                OriginalPath = _directoryPath = prototype.OriginalPath;
                break;
            case ContainerType.Archive:
                OriginalPath = _archivePath = prototype.OriginalPath;
                break;
            default:
                throw new ArgumentException($"{nameof(_3DModel)} must reference either directory or archive.", nameof(prototype));
        }
        _disposeTemps = prototype._disposeTemps;
    }

    #endregion

    internal static IEnumerable<_3DModel> EnumerateIn(string directoryPath, bool disposeTemps = true)
    {
        var _3DModelContainers = Directory.EnumerateDirectories(directoryPath).ToList();
        _3DModelContainers.AddRange(Archive.EnumerateIn(directoryPath));

        return _3DModelContainers.Select(containerPath => new _3DModel(containerPath, disposeTemps));
    }

    #region IDisposable

    public void Dispose()
    { Dispose(true); GC.SuppressFinalize(this); }

    protected void Dispose(bool managed)
    {
        if (managed)
        {
            if (_disposeTemps)
            {
                if (!_isDisposed)
                {
                    switch (OriginalContainer)
                    {
                        case ContainerType.Archive:
                            try { new DirectoryInfo(_directoryPath!).Delete(DeletionMode.Wipe); } catch { };
                            break;
                        case ContainerType.Directory:
                            try { File.Delete(_archivePath!); } catch { };
                            break;
                    }

                    _isDisposed = true;
                }
            }
        }
    }
    bool _isDisposed;
    readonly bool _disposeTemps;

    #endregion

    public static implicit operator _3DModel(string containerPath) =>
        new(containerPath);


    internal enum ContainerType
    { Archive, Directory }

    internal static class Archive
    {
        internal static IEnumerable<string> EnumerateIn(string directoryPath) =>
            Directory.EnumerateFiles(directoryPath).Where(HasValidExtension);

        internal static bool Exists(string path) => File.Exists(path) && HasValidExtension(path);

        static bool HasValidExtension(string pathOrExtension) =>
            _validExtensions.Contains(Path.GetExtension(pathOrExtension));

        readonly static string[] _validExtensions = { ".zip", ".rar" };
    }
}
