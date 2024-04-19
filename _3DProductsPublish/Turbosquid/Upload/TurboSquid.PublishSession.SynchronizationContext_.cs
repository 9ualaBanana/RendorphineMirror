#define ENABLE_PARALLELIZATION

using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid.Upload.Processing;
using static _3DProductsPublish._3DProductDS._3DProduct;

namespace _3DProductsPublish.Turbosquid.Upload;

public partial class TurboSquid
{
    internal partial class PublishSession
    {
        class SynchronizationContext_(PublishSession session)
        {
            static readonly Logger _logger = LogManager.GetCurrentClassLogger();

            PublishSession Session { get; init; } = session;

            internal async Task DesynchronizeOutdatedAssetsAsync<TAsset>() where TAsset : ITurboSquidProcessed3DProductAsset
            {
                _logger.Trace($"Desynchronizing outdated {typeof(TAsset).Name} assets.");
                await Parallel.ForEachAsync((typeof(TAsset) switch
                {
                    _ when typeof(TAsset) == typeof(TurboSquidProcessed3DModel) => Session._3DProduct._3DModels.OfType<TAsset>(),
                    _ when typeof(TAsset) == typeof(TurboSquidProcessed3DProductThumbnail) => Session._3DProduct.Thumbnails.OfType<TAsset>(),
                    _ when typeof(TAsset) == typeof(TurboSquidProcessed3DProductTextures) => Session._3DProduct.Textures.OfType<TAsset>(),
                    _ => throw new NotImplementedException()
                })
                .Where(asset => asset switch 
                {
                    TurboSquidProcessed3DModel _3DModel => File.GetLastWriteTimeUtc(_3DModel.Archived) > Session._3DProduct.Tracker.Model(_3DModel).LastWriteTime,
                    TurboSquidProcessed3DProductThumbnail thumbnail => File.GetLastWriteTimeUtc(thumbnail.Path) > Session._3DProduct.Tracker.Preview(thumbnail).LastWriteTime,
                    _ => throw new NotImplementedException()
                }),

                async (asset, _) => await DesynchronizeAsync(asset));
                _logger.Trace($"Desynchronization of outdated {typeof(TAsset).Name} assets completed.");


                async Task DesynchronizeAsync(TAsset asset)
                {
                    await DeleteAssetAsync(asset);
                    Desynchronize(asset);
                    Session._3DProduct.Tracker.Write();
                }
            }

            async Task DeleteAssetAsync(ITurboSquidProcessed3DProductAsset asset)
            {
                switch (asset)
                {
                    case TurboSquidProcessed3DModel model:
                        await DeleteAssetAsync(model, "product_files", "product_file"); break;
                    case TurboSquidProcessed3DProductThumbnail preview:
                        await DeleteAssetAsync(preview, "thumbnails", "thumbnail"); break;
                    case TurboSquidProcessed3DProductTextures textures:
                        await DeleteAssetAsync(textures, "associated_files", "texture_file"); break;
                };
            }
            async Task DeleteAssetAsync<TAsset>(ITurboSquidProcessed3DProductAsset<TAsset> processedAsset, string resource, string type)
                where TAsset : I3DProductAsset
            {
                try
                {
                    _logger.Debug($"Deleting {processedAsset.Name()} from remote.");
                    await Session.Client.SendAsync(
                        new HttpRequestMessage(
                            HttpMethod.Delete,

                            $"turbosquid/products/{Session._3DProduct.Tracker.Data.ProductID}/{resource}/{processedAsset.FileId}")
                        { Content = MetadataForm() },
                        Session.CancellationToken);
                    _logger.Debug($"{processedAsset.Name()} asset has been deleted from remote.");
                }
                catch (Exception ex)
                { throw new HttpRequestException($"{processedAsset.Name()} asset deletion from remote failed.", ex); }


                StringContent MetadataForm()
                {
                    // Explicit conversions of numbers to strings are required.
                    var metadataForm = new JObject(
                        new JProperty("authenticity_token", Session.Client.Credential.AuthenticityToken),
                        new JProperty("draft_id", Session._3DProduct.Tracker.Data.DraftID.ToString()),
                        new JProperty("id", processedAsset.FileId.ToString()),
                        new JProperty("product_id", Session._3DProduct.Tracker.Data.ProductID.ToString()),
                        new JProperty("type", type));

                    return metadataForm.ToJsonContent();
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
            {
                Session._3DProduct._3DModels.Remove((_3DModel)_.Asset); Session._3DProduct._3DModels.Add((TurboSquidProcessed3DModel)_);
                Session._3DProduct.Tracker.Model(_).Update(_.FileId);
            }
            void Synchronize(TurboSquidProcessed3DProductThumbnail _)
            {
                Session._3DProduct.Thumbnails.Remove((_3DProductThumbnail)_.Asset); Session._3DProduct.Thumbnails.Add((TurboSquidProcessed3DProductThumbnail)_);
                Session._3DProduct.Tracker.Preview(_).Update(_.FileId);
            }
            void Synchronize(TurboSquidProcessed3DProductTextures _)
            { Session._3DProduct.Textures.Remove((Textures_)_.Asset); Session._3DProduct.Textures.Add((TurboSquidProcessed3DProductTextures)_); }

            internal void Desynchronize(ITurboSquidProcessed3DProductAsset asset)
            {
                switch (asset)
                {
                    case TurboSquidProcessed3DModel model:
                        Desynchronize(model);
                        break;
                    case TurboSquidProcessed3DProductThumbnail thumbnail:
                        Desynchronize(thumbnail);
                        break;
                    case TurboSquidProcessed3DProductTextures textures:
                        Desynchronize(textures);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported type of {nameof(ITurboSquidProcessed3DProductAsset)}: {asset}");
                }
            }
            void Desynchronize(TurboSquidProcessed3DModel _)
            { 
                Session._3DProduct._3DModels.Remove((TurboSquidProcessed3DModel)_); Session._3DProduct._3DModels.Add((_3DModel)_.Asset);
                Session._3DProduct.Tracker.Model(_).Update(id: default);
            }
            void Desynchronize(TurboSquidProcessed3DProductThumbnail _)
            {
                Session._3DProduct.Thumbnails.Remove((TurboSquidProcessed3DProductThumbnail)_); Session._3DProduct.Thumbnails.Add((_3DProductThumbnail)_.Asset);
                Session._3DProduct.Tracker.Preview(_).Update(id: default);
            }
            void Desynchronize(TurboSquidProcessed3DProductTextures _)
            { Session._3DProduct.Textures.Remove((TurboSquidProcessed3DProductTextures)_); Session._3DProduct.Textures.Add((Textures_)_.Asset); }
        }
    }
}
