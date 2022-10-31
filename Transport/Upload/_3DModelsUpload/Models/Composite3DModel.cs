namespace Transport.Upload._3DModelsUpload.Models;

public record Composite3DModel : IDisposable
{
    public IEnumerable<string> Previews { get; init; }
    public IEnumerable<_3DModel> _3DModels { get; init; }

    #region Initialization

    public static Composite3DModel FromDirectory(string directory) => new(
        Composite3DModelPreview._EnumerateIn(directory),
        _3DModel._EnumerateIn(directory)
            .Select(_3DModelContainer => _3DModel.FromContainer(_3DModelContainer.OriginalPath))
            .ToArray());

    Composite3DModel(IEnumerable<string>? previews, params _3DModel[] _3DModels)
    {
        Previews = previews ?? Enumerable.Empty<string>();
        this._3DModels = _3DModels;
    }

    #endregion

    /// <remarks>
    /// Should only be called from _3DModelUploader as a preliminary to the actual upload.
    /// </remarks>
    internal void Archive() { foreach (var _3DModel in _3DModels) _3DModel.Archive(); }

    #region IDisposable

    ~Composite3DModel() => Dispose(false);

    public void Dispose()
    { Dispose(true); GC.SuppressFinalize(this); }

    protected void Dispose(bool _)
    {
        if (isDisposed) return;

        foreach (var _3DModel in _3DModels)
            _3DModel.Dispose();

        isDisposed = true;
    }
    bool isDisposed;

    #endregion
}
