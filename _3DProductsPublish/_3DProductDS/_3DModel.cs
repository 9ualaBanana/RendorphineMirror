﻿using _3DProductsPublish.Turbosquid._3DModelComponents;

namespace _3DProductsPublish._3DProductDS;

public partial record _3DModel(string Path) : I3DProductAsset, IDisposable
{
    public static implicit operator string(_3DModel model) => model.Path;
    public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);

    internal string Archived => _archived ??= AssetContainer.Archive_.Pack(this, true);
    string? _archived;


    // Currently pre-archived _3DModels are not supported.
    internal static IEnumerable<_3DModel> EnumerateAt(string directoryPath)
        => Directory.EnumerateFiles(directoryPath)
        .Where(_ => FileFormat_.Dictionary.ContainsKey(System.IO.Path.GetExtension(_).ToLowerInvariant()))
        .Select(_ => new _3DModel(_));

    #region IDisposable

    public void Dispose()
    { Dispose(true); GC.SuppressFinalize(this); }

    protected void Dispose(bool managed)
    {
        if (managed)
        {
            if (!_isDisposed)
            {
                if (_archived is not null)
                    File.Delete(_archived);

                _isDisposed = true;
            }
        }
    }
    bool _isDisposed;

    #endregion

    public interface IMetadata { string Name { get; } }
}

public partial record _3DModel<TMetadata> : _3DModel
    where TMetadata : _3DModel.IMetadata
{
    internal readonly TMetadata Metadata;

    internal _3DModel(_3DModel _3DModel, TMetadata metadata)
        : base(_3DModel)
    {
        Metadata = metadata;
    }

    protected _3DModel(_3DModel<TMetadata> _3DModel)
        : base(_3DModel)
    {
        Metadata = _3DModel.Metadata;
    }
}
