using System.IO.Compression;

namespace Transport.Upload._3DModelsUpload;

/// <summary>
/// Wraps either directory or archive in which 3D model parts are stored.
/// </summary>
public class _3DModel : IDisposable
{
    public string OriginalPath => _directoryPath is not null ? _directoryPath : _archivePath!;

    #region ContentManagement

    /// <remarks>
    /// Creates a temporary directory where the files are extracted
    /// if the <see cref="_3DModel"/> is constructed from an archive.
    /// </remarks>
    /// <returns>Paths to files that make up the model.</returns>
    public IEnumerable<string> Files
    {
        get
        {
            if (_directoryPath is null)
            {
                _directoryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                ZipFile.ExtractToDirectory(_archivePath!, _directoryPath); _directoryIsTemp = true;
            }
            return Directory.EnumerateFiles(_directoryPath);
        }
    }
    string? _directoryPath;

    public string Archive()
    {
        if (_archivePath is not null) return _archivePath;

        DirectoryInfo temp3DModelDirectoryToArchive = new(Path.Combine(
            Path.GetTempPath(), Path.GetRandomFileName()));
        temp3DModelDirectoryToArchive.Create();

        foreach (string _3DModelPart in Files)
        {
            string _3DModelPartInTempDirectoryToArchive = Path.Combine(
                temp3DModelDirectoryToArchive.FullName, Path.GetFileName(_3DModelPart));
            File.Copy(_3DModelPart, _3DModelPartInTempDirectoryToArchive);
        }
        _archivePath = Path.ChangeExtension(temp3DModelDirectoryToArchive.FullName, ".zip");
        temp3DModelDirectoryToArchive.DeleteAfter(
            () => ZipFile.CreateFromDirectory(temp3DModelDirectoryToArchive.FullName, _archivePath)
            ); _archiveIsTemp = true;

        return _archivePath;
    }
    string? _archivePath;

    #endregion

    #region Initialization

    public static _3DModel FromContainer(string path, bool disposeTemps = true) => new(path, disposeTemps);

    _3DModel(string containerPath, bool disposeTemps = true)
    {
        if (Directory.Exists(containerPath)) _directoryPath = containerPath;
        else if (_ArchiveExists(containerPath)) _archivePath = containerPath;
        else throw new ArgumentException("Container must reference either directory or archive.", nameof(containerPath));

        _disposeTemps = disposeTemps;
    }

    static bool _ArchiveExists(string path) => File.Exists(path) && _HasValidArchiveExtension(path);

    static bool _HasValidArchiveExtension(string pathOrExtension) =>
        _allowedExtensions.Contains(Path.GetExtension(pathOrExtension));

    readonly static string[] _allowedExtensions = { ".zip", ".rar" };

    #endregion

    internal static IEnumerable<_3DModel> _EnumerateIn(string _3DModelDirectory, bool disposeTemps = true)
    {
        var _3DModelContainers = Directory.EnumerateDirectories(_3DModelDirectory).ToList();
        _3DModelContainers.AddRange(Directory.EnumerateFiles(_3DModelDirectory).Where(_HasValidArchiveExtension));

        return _3DModelContainers.Select(containerPath => new _3DModel(containerPath, disposeTemps));

    }

    #region IDisposable

    public void Dispose()
    { Dispose(true); GC.SuppressFinalize(this); }

    protected void Dispose(bool _)
    {
        if (_isDisposed) return;

        if (_disposeTemps)
        {
            if (_directoryIsTemp)
            { try { new DirectoryInfo(_directoryPath!).Delete(DeletionMode.Wipe); } catch { } }
            if (_archiveIsTemp)
            { try { File.Delete(_archivePath!); } catch { } }
        }

        _isDisposed = true;
    }
    bool _isDisposed;
    bool _disposeTemps;
    bool _directoryIsTemp;
    bool _archiveIsTemp;

    #endregion

    public static implicit operator _3DModel(string containerPath) =>
        new(containerPath);
}
