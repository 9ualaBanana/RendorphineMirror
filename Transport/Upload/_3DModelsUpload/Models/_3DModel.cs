using System.IO.Compression;

namespace Transport.Upload._3DModelsUpload.Models;

public class _3DModel
{
    // Either `_3DModeParts` or `_archive` is always not null.
    internal IEnumerable<string> _ToModelParts()
    {
        if (_3DModelParts is not null) return _3DModelParts;

        ZipFile.ExtractToDirectory(_archive!, _3DModelPartsDirectory.FullName);
        return Directory.EnumerateFiles(_3DModelPartsDirectory.FullName);
    }
    readonly List<string>? _3DModelParts;
    // DirectoryInfo _3DModelPartsDirectory = _3DModelParts is not null && _3DModelParts.Any() ?
    // new Path.GetDirectoryName(_3DModelParts.First()) : new(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
    DirectoryInfo _3DModelPartsDirectory = new(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

    internal string _ToArchive()
    {
        if (_archive is not null) return _archive;

        var temp3DModelsDirectoryToArchive = new DirectoryInfo(Path.Combine(
            Path.GetTempPath(), Path.GetRandomFileName()));
        foreach (var _3DModelPart in _3DModelParts!)
        {
            string _3DModelPartInDirectory = Path.Combine(temp3DModelsDirectoryToArchive.FullName, Path.GetFileName(_3DModelPart));
            File.Copy(_3DModelPart, _3DModelPartInDirectory);
        }
        string archiveName = Path.ChangeExtension(temp3DModelsDirectoryToArchive.FullName, ".zip");
        temp3DModelsDirectoryToArchive.DeleteAfter(
            () => ZipFile.CreateFromDirectory(temp3DModelsDirectoryToArchive.FullName, archiveName));

        return _archive = archiveName;
    }
    string? _archive;
    string? _directory;

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
        if (isArchive) _archive = _ValidateExtension(container);
        else if (!Directory.Exists(_directory = container))
            throw new DirectoryNotFoundException("Directory containing 3D model doesn't exist.");
    }

    internal static IEnumerable<string> _ValidateExtensions(IEnumerable<string>? pathsOrExtensions)
    {
        if (pathsOrExtensions is null) return Enumerable.Empty<string>();

        foreach (var pathOrExtension in pathsOrExtensions)
            _ValidateExtension(pathOrExtension);
        
        return pathsOrExtensions; }

    internal static string _ValidateExtension(string pathOrExtension)
    {
        if (!HasValidExtension(pathOrExtension))
            throw new ArgumentException(
                $"The path doesn't reference any of the supported archives: {string.Join(", ", _allowedExtensions)}.",
                nameof(pathOrExtension));
        else return pathOrExtension;
    }

    #endregion

    internal static bool HasValidExtension(string pathOrExtension) =>
        _allowedExtensions.Contains(Path.GetExtension(pathOrExtension));

    static readonly string[] _allowedExtensions = { ".zip", ".rar" };
}
