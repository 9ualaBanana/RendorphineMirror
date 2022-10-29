﻿namespace Transport.Upload._3DModelsUpload.Models;

public record Composite3DModel
{
    public IEnumerable<string> Previews { get; init; }
    public IEnumerable<_3DModel> _3DModels { get; init; }

    #region Initialization

    public Composite3DModel(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"The directory doesn't exist at {directoryPath}.");

        Previews = Composite3DModelPreview._ValidateExtensions(
            Directory.EnumerateFiles(directoryPath).Where(Composite3DModelPreview._HasValidExtension));
        var _3DModelDirectories = Directory.EnumerateDirectories(directoryPath);
        _3DModels = _3DModelDirectories.Select(Directory.EnumerateFiles).Select(_3DModelParts => new _3DModel(_3DModelParts));
    }
    
    /// <remarks>
    /// <paramref name="_3DModelsInDirectories"/> must be the paths to directories (not archives) representing 3D models.
    /// </remarks>
    /// <param name="_3DModelsInDirectories">The directories representing 3D models.</param>
    public Composite3DModel(IEnumerable<string>? previews = null, params string[] _3DModelsInDirectories)
        : this(previews, _3DModelsInDirectories.Select(Directory.EnumerateFiles).ToArray())
    {
    }

    public Composite3DModel(IEnumerable<string>? previews = null, params IEnumerable<string>[] _3DModelsAsParts)
    {
        Previews = Composite3DModelPreview._ValidateExtensions(previews);
        _3DModels = _3DModelsAsParts.Select(_3DModelParts => new _3DModel(_3DModelParts));
    }

    #endregion

    // Should only be called from _3DModelUploader as a preliminary to the actual upload.
    internal void Archive() { foreach (var _3DModel in _3DModels) _3DModel._ToArchive(); }
}
