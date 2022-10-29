namespace Transport.Upload._3DModelsUpload.Models;

public record Composite3DModel
{
    public IEnumerable<string> Previews { get; init; }
    public IEnumerable<_3DModel> _3DModels { get; init; }

    #region Initialization

    /// <remarks>
    /// Nested models inside <paramref name="composite3DModelInDirectory"/> must be contained inside directories (not archives).
    /// </remarks>
    public Composite3DModel(string composite3DModelInDirectory) : this(
        Directory.EnumerateFiles(composite3DModelInDirectory).Where(Composite3DModelPreview._HasValidExtension),
        Directory.EnumerateDirectories(composite3DModelInDirectory).ToArray())
    {
    }

    /// <remarks>
    /// <paramref name="_3DModelsInDirectories"/> must be the paths to directories (not archives) representing 3D models.
    /// </remarks>
    /// <param name="_3DModelsInDirectories">The directories representing 3D models.</param>
    public Composite3DModel(IEnumerable<string>? previews = null, params string[] _3DModelsInDirectories)
        : this(previews, _3DModelsInDirectories.Select(_3DModelInDirectory => new _3DModel(_3DModelInDirectory)).ToArray())
    {
    }
    
    public Composite3DModel(IEnumerable<string>? previews = null, params _3DModel[] _3DModels)
    {
        Previews = Composite3DModelPreview._ValidateExtensions(previews);
        this._3DModels = _3DModels;
    }

    #endregion

    // Should only be called from _3DModelUploader as a preliminary to the actual upload.
    internal void Archive() { foreach (var _3DModel in _3DModels) _3DModel._ToArchive(); }
}
