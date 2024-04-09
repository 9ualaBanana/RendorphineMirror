using _3DProductsPublish.Turbosquid.Upload.Processing;

namespace _3DProductsPublish.Turbosquid.Upload;

public partial class TurboSquid
{
    public partial record _3DProduct : _3DProductDS._3DProduct
    {
        public Metadata__ Metadata { get; }
        public Tracker_ Tracker { get; init; }

        public _3DProduct(_3DProductDS._3DProduct _3DProduct, Metadata__ metadata)
            : base(_3DProduct)
        {
            Metadata = metadata;
            Tracker = new(this);
        }

        /// <remarks>
        /// Basically upcasts assets to ITurboSquidProcessed3DProductAssets if meta.json stores their remote IDs.
        /// Seems like a convinient in-code way to differentiate processed and unprocessed assets without querying Tracker.Data.
        /// </remarks>
        internal void Synchronize()
        {
            Synchronize3DModels();
            SynchronizePreviews();
            //SynchronizeTextures();


            // Synchronize with remote based on ID?
            void Synchronize3DModels()
                => _3DModels.ToList().ForEach(_3DModel =>
                {
                    if (Tracker.Model(_3DModel).ID is long id and not 0)
                        _3DModels[_3DModels.IndexOf(_3DModel)] = new TurboSquidProcessed3DModel(_3DModel, id);
                });

            void SynchronizePreviews()
                => Thumbnails.ToList().ForEach(preview =>
                {
                    if (Tracker.Preview(preview).ID is long id and not 0)
                        Thumbnails[Thumbnails.IndexOf(preview)] = new TurboSquidProcessed3DProductThumbnail(preview, id);
                });

            void SynchronizeTextures()
            {
            }
        }
    }
}
