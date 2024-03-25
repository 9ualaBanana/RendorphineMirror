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
        public readonly Metadata__ Metadata;

        public _3DProduct(_3DProductDS._3DProduct _3DProduct, Metadata__ metadata)
            : base(_3DProduct)
        {
            // Metadata must be set befor messing with metadata file?
            Metadata = metadata;
            var turboSquidMetadataFile = Metadata__.File.For(this);
            var meta = turboSquidMetadataFile.Read();
            ID = meta.ProductID;
            DraftID = meta.DraftID;
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

        internal void Synchronize(IEnumerable<ITurboSquidProcessed3DProductAsset> assets)
        { foreach (var asset in assets) Synchronize(asset); }
        internal void Synchronize(ITurboSquidProcessed3DProductAsset asset)
        {
            switch (asset)
            {
                case TurboSquidProcessed3DModel model:
                    Synchronize(model);
                    break;
                case TurboSquidProcessed3DProductThumbnail thumbnail:
                    Synchronize(thumbnail);
                    break;
                case TurboSquidProcessed3DProductTextures textures:
                    Synchronize(textures);
                    break;
                default:
                    throw new ArgumentException($"Unsupported type of {nameof(ITurboSquidProcessed3DProductAsset)}: {asset}");
            }
        }
        void Synchronize(TurboSquidProcessed3DModel _)
        { _3DModels.Remove((_3DModel<TurboSquid3DModelMetadata>)_.Asset); _3DModels.Add((TurboSquidProcessed3DModel)_); }
        void Synchronize(TurboSquidProcessed3DProductThumbnail _)
        { Thumbnails.Remove((_3DProductThumbnail)_.Asset); Thumbnails.Add((TurboSquidProcessed3DProductThumbnail)_); }
        void Synchronize(TurboSquidProcessed3DProductTextures _)
        { Textures.Remove((Textures_)_.Asset); Textures.Add((TurboSquidProcessed3DProductTextures)_); }

        internal void Desynchronize(TurboSquidProcessed3DModel _)
        { _3DModels.Remove((TurboSquidProcessed3DModel)_); _3DModels.Add((_3DModel<TurboSquid3DModelMetadata>)_.Asset); }
        internal void Desynchronize(TurboSquidProcessed3DProductThumbnail _)
        { Thumbnails.Remove((TurboSquidProcessed3DProductThumbnail)_); Thumbnails.Add((_3DProductThumbnail)_.Asset); }


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
