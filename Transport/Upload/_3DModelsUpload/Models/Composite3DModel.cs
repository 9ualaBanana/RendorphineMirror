namespace Transport.Upload._3DModelsUpload.Models;

public record Composite3DModel : IDisposable
{
    public IEnumerable<string> Previews { get; init; }
    public IEnumerable<_3DModel> _3DModels { get; init; }

    #region Initialization

    public static Composite3DModel FromDirectory(string directory)
    {
        var previews = Directory.EnumerateFiles(directory).Where(Composite3DModelPreview._HasValidExtension);
        var _3DModelsInContainers = Directory.EnumerateDirectories(directory).ToList();
        _3DModelsInContainers.AddRange(Directory.EnumerateFiles(directory).Where(_3DModel._HasValidArchiveExtension));

        return new(previews, _3DModelsInContainers.ToArray());
    }

    public Composite3DModel(IEnumerable<string>? previews = null, params string[] _3DModelsInContainers)
        : this(previews, _3DModelsInContainers.Select(_3DModel.FromContainer).ToArray())
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
