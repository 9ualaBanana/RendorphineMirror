using System.IO.Compression;

namespace Transport.Upload._3DModelsUpload.Models;

internal class _3DModel
{
    internal IEnumerable<string> _AsModelParts
    {
        get
        {
            if (_modelParts is not null) return _modelParts;

            ZipFile.ExtractToDirectory(_archive!, _modelPartsDirectory.FullName);
            return Directory.EnumerateFiles(_modelPartsDirectory.FullName);
        }
    }
    readonly List<string>? _modelParts;
    DirectoryInfo _modelPartsDirectory = new(Path.Combine(
        Path.GetTempPath(), Path.GetRandomFileName()));

    internal string _AsArchive
    {
        get
        {
            if (_archive is not null) return _archive;

            var directory = new DirectoryInfo(Path.Combine(
                Path.GetTempPath(), Path.GetRandomFileName()));
            foreach (var modelPart in _modelParts!)
            {
                string modelPartInDirectory = Path.Combine(directory.FullName, Path.GetFileName(modelPart));
                File.Copy(modelPart, modelPartInDirectory);
            }
            string archiveName = Path.ChangeExtension(directory.FullName, ".zip");
            directory.DeleteAfter(
                () => ZipFile.CreateFromDirectory(directory.FullName, archiveName));

            return _archive = archiveName;
        }
    }
    string? _archive;

    #region Initialization

    internal _3DModel(string archivedModel)
    {
        _ValidateExtension(archivedModel);
        _archive = archivedModel;
    }

    internal _3DModel(IEnumerable<string> modelParts)
    {
        _modelParts = modelParts.ToList();
    }

    static void _ValidateExtension(string pathOrExtension)
    {
        if (!HasValidExtension(pathOrExtension))
            throw new ArgumentException(
                $"The path doesn't reference any of the supported archives: {string.Join(", ", _allowedExtensions)}.",
                nameof(pathOrExtension));
    }

    internal static bool HasValidExtension(string pathOrExtension) => _allowedExtensions.Contains(pathOrExtension);

    static readonly string[] _allowedExtensions = { ".zip", ".rar" };

    #endregion

    public static implicit operator _3DModel(string archivedModel) => new(archivedModel);
}
