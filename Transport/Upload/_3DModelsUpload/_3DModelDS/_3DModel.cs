using System.IO.Compression;

namespace Transport.Upload._3DModelsUpload._3DModelDS;

/// <summary>
/// Wraps either directory or archive in which 3D model parts are stored.
/// </summary>
public class _3DModel : IDisposable
{
    /// <summary>
    /// Refers to the path from which this <see cref="_3DModel"/> was initialized.
    /// </summary>
    public string OriginalPath => _directoryPath is not null ? _directoryPath : _archivePath!;
    string? _directoryPath;
    string? _archivePath;

    #region ContentManagement

    /// <summary>
    /// Creates a temporary archive to which the files are extracted if the <see cref="_3DModel"/>
    /// is initialized from a directory (i.e. <see cref="OriginalPath"/> referes to a directory).
    /// </summary>
    /// <returns>Path to the archive where this <see cref="_3DModel"/> is stored.</returns>
    public string Archive()
    {
        if (_archivePath is null)
        {
            _archivePath = _3DModelArchiver._Archive(this);
            _archiveIsTemp = true;
        }

        return _archivePath;
    }

    /// <remarks>
    /// Creates a temporary directory to which the files are extracted if the <see cref="_3DModel"/>
    /// is initialized from an archive (i.e. <see cref="OriginalPath"/> refers an archive).
    /// </remarks>
    /// <returns>Paths of files that make up this <see cref="_3DModel"/>.</returns>
    public IEnumerable<string> Files
    {
        get
        {
            if (_directoryPath is null)
            {
                _directoryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                ZipFile.ExtractToDirectory(_archivePath!, _directoryPath);
                _directoryIsTemp = true;
            }

            return Directory.EnumerateFiles(_directoryPath);
        }
    }

    #endregion

    #region Initialization

    public static _3DModel FromContainer(string path, bool disposeTemps = true) => new(path, disposeTemps);

    _3DModel(string containerPath, bool disposeTemps = true)
    {
        if (Directory.Exists(containerPath))
            _directoryPath = containerPath;
        else if (_3DModelArchive.Exists(containerPath))
            _archivePath = containerPath;
        else throw new ArgumentException("Container must reference either directory or archive.", nameof(containerPath));

        _disposeTemps = disposeTemps;
    }

    #endregion

    internal static IEnumerable<_3DModel> _EnumerateIn(string directoryPath, bool disposeTemps = true)
    {
        var _3DModelContainers = Directory.EnumerateDirectories(directoryPath).ToList();
        _3DModelContainers.AddRange(_3DModelArchive._EnumerateIn(directoryPath));

        return _3DModelContainers.Select(containerPath => new _3DModel(containerPath, disposeTemps));
    }

    #region IDisposable

    public void Dispose()
    { Dispose(true); GC.SuppressFinalize(this); }

    protected void Dispose(bool managed)
    {
        if (_isDisposed) return;

        if (managed)
        {
            if (_disposeTemps)
            {
                if (_directoryIsTemp)
                { try { new DirectoryInfo(_directoryPath!).Delete(DeletionMode.Wipe); } catch { } }
                if (_archiveIsTemp)
                { try { File.Delete(_archivePath!); } catch { } }
            }
        }

        _isDisposed = true;
    }
    bool _isDisposed;
    readonly bool _disposeTemps;
    bool _directoryIsTemp;
    bool _archiveIsTemp;

    #endregion

    public static implicit operator _3DModel(string containerPath) =>
        new(containerPath);
}
