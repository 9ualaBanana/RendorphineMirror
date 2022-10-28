using System.IO.Compression;

namespace Transport.Upload._3DModelsUpload.Models;

internal class _3DModel
{
    internal IEnumerable<string> _ToModelParts()
    {
        if (_3dModelParts is not null) return _3dModelParts;

        ZipFile.ExtractToDirectory(_archive!, _3dModelPartsDirectory.FullName);
        return Directory.EnumerateFiles(_3dModelPartsDirectory.FullName);
    }
    readonly List<string>? _3dModelParts;
    DirectoryInfo _3dModelPartsDirectory = new(Path.Combine(
        Path.GetTempPath(), Path.GetRandomFileName()));

    internal string _ToArchive()
    {
        if (_archive is not null) return _archive;

        var directory = new DirectoryInfo(Path.Combine(
            Path.GetTempPath(), Path.GetRandomFileName()));
        foreach (var _3dModelPart in _3dModelParts!)
        {
            string modelPartInDirectory = Path.Combine(directory.FullName, Path.GetFileName(_3dModelPart));
            File.Copy(_3dModelPart, modelPartInDirectory);
        }
        string archiveName = Path.ChangeExtension(directory.FullName, ".zip");
        directory.DeleteAfter(
            () => ZipFile.CreateFromDirectory(directory.FullName, archiveName));

        return _archive = archiveName;
    }
    string? _archive;

    #region Initialization

    internal _3DModel(string archived3DModel)
    {
        _ValidateExtension(archived3DModel);
        _archive = archived3DModel;
    }

    internal _3DModel(IEnumerable<string> _3dModelParts)
    {
        this._3dModelParts = _3dModelParts.ToList();
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


    public static implicit operator _3DModel(string archivedModel) => new(archivedModel);
}
