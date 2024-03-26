using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Upload.Processing;
using _3DProductsPublish._3DProductDS;

namespace _3DProductsPublish.Turbosquid.Upload;

public partial class TurboSquid
{
    public partial record _3DProduct : _3DProductDS._3DProduct
    {
        public long ID { get; internal set; } = default;
        internal long DraftID { get; set; } = default!;
        new public List<_3DModel<TurboSquid3DModelMetadata>> _3DModels { get; internal set; }
        public Metadata__ Metadata { get; }

        public _3DProduct(_3DProductDS._3DProduct _3DProduct, Metadata__ metadata)
            : base(_3DProduct)
        {
            // Metadata must be set befor messing with metadata file?
            Metadata = metadata;
            var meta = Metadata__.File.For(this).Read();
            ID = meta.ProductID;
            DraftID = meta.DraftID;
            _3DModels = base._3DModels.Join(meta.Models,
                _3DModel => _3DModel.Name(),
                metadata => metadata.Name,  // IMetadata.Name is used here.
                (_3DModel, metadata) => new _3DModel<TurboSquid3DModelMetadata>(_3DModel, metadata))
                .ToList();
            foreach (var _ in _3DProduct.Thumbnails.Join(meta.Previews,
                preview => preview.Name(),
                metadata => metadata.Name,
                (preview, metadata) => new { preview, metadata }))
                _.preview.LastWriteTime = _.metadata.LastWriteTime;
        }

        /// <summary>
        /// Binds remote asset ID to the local asset with the same name.
        /// </summary>
        internal _3DProduct SynchronizedWith(Remote remote)
        {
            Synchronize3DModels();
            SynchronizePreviews();
            SynchronizeTextures();
            return this;


            void Synchronize3DModels()
            {
                foreach (var _3DModel in remote.models.Join(_3DModels,
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
                    _ => Path.GetFileName(_.Path),
                    (remote, local) => new { _ = local, remote.id }))
                {
                    if (Thumbnails.IndexOf(preview._) is int index and not -1)
                        Thumbnails[index] = new TurboSquidProcessed3DProductThumbnail(preview._, preview.id);
                }
            }

            void SynchronizeTextures()
            {
                foreach (var textures in remote.texture_files.Join(Textures,
                    _ => _.attributes.name,
                    _ => Path.GetFileName(_.Path),
                    (remote, local) => new { _ = local, remote.id }))
                {
                    if (Textures.IndexOf(textures._) is int index and not -1)
                        Textures[index] = new TurboSquidProcessed3DProductTextures(textures._, textures.id);
                }
            }
        }


        internal record Draft
        {
            internal TurboSquidAwsSession AWS { get; init; }
            internal _3DProduct LocalProduct { get; init; }
            internal _3DProduct.Remote RemoteProduct { get; init; }

            public Draft(TurboSquidAwsSession awsSession, _3DProduct localProduct, _3DProduct.Remote remoteProduct)
            {
                AWS = awsSession;
                RemoteProduct = remoteProduct;
                LocalProduct = remoteProduct is null ? localProduct : localProduct.SynchronizedWith(remoteProduct);
            }

            internal IEnumerable<TurboSquidProcessed3DModel> Edited3DModels
                => Synchronized3DModels.Join(RemoteProduct.models,
                    local => local.FileId,
                    remote => remote.id,
                    (local, remote) => new { Local = local, Remote = remote })
                .Where(_ => !_.Remote.Equals(_.Local))
                .Select(_ => _.Local);

            IEnumerable<TurboSquidProcessed3DModel> Synchronized3DModels
            {
                get
                {
                    try { return LocalProduct._3DModels.Cast<TurboSquidProcessed3DModel>(); }
                    catch (InvalidCastException)
                    { throw new InvalidOperationException($"Local {nameof(_3DModel)}s were not synchronized."); }
                }
            }
        }
    }
}
