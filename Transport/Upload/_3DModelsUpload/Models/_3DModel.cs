using System.IO.Compression;

namespace Transport.Upload._3DModelsUpload.Models;

public class _3DModel
{
    internal IEnumerable<string> _ToModelParts()
    {
        if (_3DModelParts is not null) return _3DModelParts;

        ZipFile.ExtractToDirectory(_archive!, _3DModelPartsDirectory.FullName);
        return Directory.EnumerateFiles(_3DModelPartsDirectory.FullName);
    }
    readonly List<string>? _3DModelParts;
    DirectoryInfo _3DModelPartsDirectory = new(Path.Combine(
        Path.GetTempPath(), Path.GetRandomFileName()));

    internal string _ToArchive()
    {
        if (_archive is not null) return _archive;

        var directory = new DirectoryInfo(Path.Combine(
            Path.GetTempPath(), Path.GetRandomFileName()));
        foreach (var _3DModelPart in _3DModelParts!)
        {
            string _3DModelPartInDirectory = Path.Combine(directory.FullName, Path.GetFileName(_3DModelPart));
            File.Copy(_3DModelPart, _3DModelPartInDirectory);
        }
        string archiveName = Path.ChangeExtension(directory.FullName, ".zip");
        directory.DeleteAfter(
            () => ZipFile.CreateFromDirectory(directory.FullName, archiveName));

        return _archive = archiveName;
    }
    string? _archive;

    #region Initialization

    public _3DModel(string _3DModelInContainer)
    {
        string archiveName;
        if (Directory.Exists(_3DModelInContainer))
        {
            archiveName = Path.ChangeExtension(_3DModelInContainer, ".zip");
            ZipFile.CreateFromDirectory(_3DModelInContainer, archiveName);
        }
        else archiveName = _ValidateExtension(_3DModelInContainer);
        
        _archive = archiveName;
    }

    internal _3DModel(IEnumerable<string> _3DModelParts)
    {
        this._3DModelParts = _3DModelParts.ToList();
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
