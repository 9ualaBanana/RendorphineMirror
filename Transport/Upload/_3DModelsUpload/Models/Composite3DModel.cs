namespace Transport.Upload._3DModelsUpload.Models;

internal record Composite3DModel
{
    internal IEnumerable<string> Previews { get; init; }
    internal IEnumerable<_3DModel> _3DModels { get; init; }

    #region Initialization
    internal Composite3DModel(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"The directory doesn't exist at {directoryPath}.");

        Previews = Composite3DModelPreview._ValidatePreviews(
            Directory.EnumerateFiles(directoryPath).Where(Composite3DModelPreview._HasValidExtension));
        var _3dModelDirectories = Directory.EnumerateDirectories(directoryPath).Where(_3DModel.HasValidExtension);
        _3DModels = _3dModelDirectories.Select(Directory.EnumerateFiles).Select(_3dModelParts => new _3DModel(_3dModelParts));
    }

    internal Composite3DModel(IEnumerable<string>? previews = null, params IEnumerable<string>[] _3dModelsAsParts)
    {
        Previews = Composite3DModelPreview._ValidatePreviews(previews);
        _3DModels = _3dModelsAsParts.Select(_3dModelParts => new _3DModel(_3dModelParts));
    }
    #endregion

    internal void Archive() { foreach (var _3dModel in _3DModels) _3dModel._ToArchive(); }
}
