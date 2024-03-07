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
            if (!File.Exists(turboSquidMetadataFile.Path))
                File.Create(turboSquidMetadataFile.Path).Dispose();
            var meta = turboSquidMetadataFile.Read();
            ID = meta.ProductID;
            DraftID = meta.DraftID;
        }

        // Remote asset data is matched to the local based on the file name which must remain the same from the moment it was assigned the remote ID.
        internal _3DProduct SynchronizedWith(Remote remote)
        {
            Synchronize3DModels();
            SynchronizePreviews();
            SynchronizeTextures();
            return this;


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
                default:
                    throw new ArgumentException($"Unsupported type of {nameof(ITurboSquidProcessed3DProductAsset)}: {asset}");
            }
        }
        void Synchronize(TurboSquidProcessed3DModel _)
        { _3DModels.Remove((_3DModel<TurboSquid3DModelMetadata>)_.Asset); _3DModels.Add((TurboSquidProcessed3DModel)_); }
        void Synchronize(TurboSquidProcessed3DProductThumbnail _)
        { Thumbnails.Remove((_3DProductThumbnail)_.Asset); Thumbnails.Add((TurboSquidProcessed3DProductThumbnail)_); }


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
