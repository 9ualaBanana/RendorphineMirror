﻿using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Upload.Processing;

namespace _3DProductsPublish._3DProductDS;

public partial record _3DProduct : IDisposable
{
    /// <summary>
    /// Path to the directory from which this <see cref="_3DProduct"/> instance was initialized.
    /// </summary>
    public readonly string ContainerPath;
    public List<_3DModel> _3DModels { get; }
    public List<_3DProductThumbnail> Thumbnails { get; internal set; }
    public Textures_? Textures { get; }

    #region Initialization

    public static _3DProduct FromDirectory(string directoryPath) => new(
        directoryPath,
        _3DModel.EnumerateAt(directoryPath).ToList(),
        _3DProductThumbnail.EnumerateAt(directoryPath).ToList(),
        Textures_.FindAt(directoryPath));

    _3DProduct(
        string containerPath,
        List<_3DModel> _3DModels,
        List<_3DProductThumbnail> thumbnails,
        Textures_? textures = null)
    {
        ContainerPath = containerPath;
        this._3DModels = _3DModels;
        Thumbnails = thumbnails;
        Textures = textures;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    { Dispose(true); GC.SuppressFinalize(this); }

    protected void Dispose(bool managed)
    {
        if (managed)
        {
            if (!_isDisposed)
            {
                foreach (var _3DModel in _3DModels)
                    _3DModel.Dispose();

                _isDisposed = true;
            }
        }
    }
    bool _isDisposed;

    #endregion
}

public record _3DProduct<TMetadata> : _3DProduct
{
    internal _3DProduct(_3DProduct _3DProduct, TMetadata metadata)
        : base(_3DProduct)
    { Metadata = metadata; }
    public readonly TMetadata Metadata;
}

public record _3DProduct<TProductMetadata, TModelsMetadata> : _3DProduct<TProductMetadata>
    where TModelsMetadata : _3DModel.IMetadata
{
    new public List<_3DModel<TModelsMetadata>> _3DModels { get; }
    public int ID { get; internal set; } = default;

    internal _3DProduct(_3DProduct<TProductMetadata> _3DProduct, IEnumerable<TModelsMetadata> modelsMetadata)
        : base(_3DProduct)
    {
        _3DModels = _3DProduct._3DModels.Join(modelsMetadata,
            _3DModel => _3DModel.Name,
            metadata => metadata.Name,  // IMetadata.Name is used here.
            (_3DModel, metadata) => new _3DModel<TModelsMetadata>(_3DModel, metadata))
            .ToList();
    }
}

public record TurboSquid3DProduct : _3DProduct<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata>
{
    internal TurboSquid3DProduct(_3DProduct<TurboSquid3DProductMetadata> _3DProduct, IEnumerable<TurboSquid3DModelMetadata> modelsMetadata)
        : base(_3DProduct, modelsMetadata)
    {
    }

    // Remote asset data is matched to the local based on the file name which must remain the same from the moment it was assigned the remote ID.
    internal TurboSquid3DProduct SynchronizedWith(TurboSquid3DProductMetadata.Product remote)
    {
        Synchronize3DModels();
        SynchronizePreviews();
        SynchronizeTextures();
        return this;


        //IEnumerable<TurboSquidProcessed3DModel> ModelsWithEditedMetadata()
        //    => _3DModels.OfType<TurboSquidProcessed3DModel>() is IEnumerable<TurboSquidProcessed3DModel> processedModels ?
        //        Remote is not null ?
        //            processedModels.Join(Remote.files.Where(_ => _.type == "product_file"),
        //            local => local.FileId,
        //            remote => remote.id,
        //            (local, remote) => new { Local = local, Remote = remote })
        //            .Where(_ => !_.Remote.Equals(_.Local))
        //            .Select(_ => _.Local) :
        //        processedModels :
        //    throw new InvalidOperationException();

        void Synchronize3DModels()
        {
            foreach (var _3DModel in remote.files.Join(_3DModels,
                _ => _.attributes.name,
                _ => Path.GetFileName(_.Archived),
                (remote, local) => new { _ = local, remote.id }))
            {
                if (_3DModels.IndexOf(_3DModel._) is int index and not -1)
                    _3DModels[index] = new TurboSquidProcessed3DModel(_3DModel._, _3DModel.id);
            }
        }

        void SynchronizePreviews()
        {
            foreach (var preview in remote.previews.Join(Thumbnails,
                _ => _.filename,
                _ => _.FileName,
                (remote, local) => new { _ = local, remote.id }))
            {
                if (Thumbnails.IndexOf(preview._) is int index and not -1)
                    Thumbnails[index] = new TurboSquidProcessed3DProductThumbnail(preview._, preview.id);
            }
        }

        void SynchronizeTextures()
        {
            //foreach (var texture in remoteProduct.files.Join(_3DProduct.Textures,
            //    _ => _.,
            //    _ => _.FileName,
            //    (remote, local) => new { remote, local }))
            //{
            //    if (_3DProduct.Thumbnails.IndexOf(texture.local) is int index and not -1)
            //        _3DProduct.Thumbnails[index] = new TurboSquidProcessed3DProductThumbnail(texture.local, texture.remote.id);
            //}
        }
    }

    internal void Synchronize(TurboSquidProcessed3DModel _)
    { _3DModels.Remove((_3DModel<TurboSquid3DModelMetadata>)_.Asset); _3DModels.Add((TurboSquidProcessed3DModel)_); }

    internal void Synchronize(TurboSquidProcessed3DProductThumbnail _)
    { Thumbnails.Remove((_3DProductThumbnail)_.Asset); Thumbnails.Add((TurboSquidProcessed3DProductThumbnail)_); }
}
