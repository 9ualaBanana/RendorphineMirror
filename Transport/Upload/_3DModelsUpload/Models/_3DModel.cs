using System.IO.Compression;

namespace Transport.Upload._3DModelsUpload.Models;

public class _3DModel : IDisposable
{
    // Either `_3DModeParts` or `_archive` is always not null.
    internal IEnumerable<string> _ToModelParts()
    {
        if (_3DModelParts is not null) return _3DModelParts;

        if (_archive is not null)
        { ZipFile.ExtractToDirectory(_archive!, _directory); _disposeDirectory = true; }

        return _3DModelParts = Directory.EnumerateFiles(_directory).ToList();
    }
    List<string>? _3DModelParts;
    string _directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    bool _disposeDirectory;

    internal string _ToArchive()
    {
        if (_archive is not null) return _archive;

        var temp3DModelsDirectoryToArchive = new DirectoryInfo(Path.Combine(
            Path.GetTempPath(), Path.GetRandomFileName()));
        temp3DModelsDirectoryToArchive.Create();

        foreach (var _3DModelPart in this._ToModelParts())
        {
            string _3DModelPartInDirectory = Path.Combine(temp3DModelsDirectoryToArchive.FullName, Path.GetFileName(_3DModelPart));
            File.Copy(_3DModelPart, _3DModelPartInDirectory);
        }
        string archiveName = Path.ChangeExtension(temp3DModelsDirectoryToArchive.FullName, ".zip");
        temp3DModelsDirectoryToArchive.DeleteAfter(
            () => ZipFile.CreateFromDirectory(temp3DModelsDirectoryToArchive.FullName, archiveName));

        _disposeArchive = true;
        return _archive = archiveName;
    }
    string? _archive;
    bool _disposeArchive;

    #region Initialization

    /// <summary>
    /// Constructs <see cref="_3DModel"/> from directory or archive.
    /// </summary>
    /// <param name="container">The directory or archive containing 3D model.</param>
    /// <returns>The 3D model constructed from the files contained inside the <paramref name="container"/>.</returns>
    public static _3DModel FromContainer(string container) => Directory.Exists(container) ?
        new(container, isArchive: false) : new(container, isArchive: true);

    /// <summary>
    /// Constructs <see cref="_3DModel"/> from directory or archive.
    /// </summary>
    /// <param name="container">The directory or archive containing 3D model.</param>
    /// <param name="isArchive">The flag indicating whether the <paramref name="container"/> is archive or directory.</param>
    /// <exception cref="DirectoryNotFoundException"></exception>
    _3DModel(string container, bool isArchive)
    {
        if (isArchive) _archive = _ValidateArchiveExtension(container);
        else if (!Directory.Exists(_directory = container))
            throw new DirectoryNotFoundException("Directory containing 3D model doesn't exist.");
    }

    internal static IEnumerable<string> _ValidateArchiveExtensions(IEnumerable<string>? pathsOrExtensions)
    {
        if (pathsOrExtensions is null) return Enumerable.Empty<string>();

        foreach (var pathOrExtension in pathsOrExtensions)
            _ValidateArchiveExtension(pathOrExtension);
        
        return pathsOrExtensions; }

    internal static string _ValidateArchiveExtension(string pathOrExtension)
    {
        if (!_HasValidArchiveExtension(pathOrExtension))
            throw new ArgumentException(
                $"The path doesn't reference any of the supported archives: {string.Join(", ", _allowedExtensions)}.",
                nameof(pathOrExtension));
        else return pathOrExtension;
    }

    #endregion

    internal static bool _HasValidArchiveExtension(string pathOrExtension) =>
        _allowedExtensions.Contains(Path.GetExtension(pathOrExtension));

    static readonly string[] _allowedExtensions = { ".zip", ".rar" };

    #region IDisposable

    ~_3DModel() => Dispose(false);

    public void Dispose()
    { Dispose(true); GC.SuppressFinalize(this); }

    protected void Dispose(bool _)
    {
        if (_isDisposed) return;

        if (_disposeArchive)
        { try { File.Delete(_archive!); } catch { } }
        if (_disposeDirectory)
        { try { new DirectoryInfo(_directory).Delete(DeletionMode.Wipe); } catch { } }

        _isDisposed = true;
    }
    bool _isDisposed;

    #endregion
}
