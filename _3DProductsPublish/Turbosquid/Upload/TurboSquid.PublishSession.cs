#define ENABLE_PARALLELIZATION

using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Upload.Processing;
using _3DProductsPublish.Turbosquid.Upload.Requests;
using MarkTM.RFProduct;
using Microsoft.Net.Http.Headers;
using System.Net.Mime;

namespace _3DProductsPublish.Turbosquid.Upload;

public partial class TurboSquid
{
    internal class PublishSession
    {
        static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        internal readonly TurboSquid._3DProduct.Draft Draft;
        internal readonly TurboSquid Client;
        internal readonly CancellationToken CancellationToken;

        internal static async Task<PublishSession> InitializeAsync(TurboSquid._3DProduct.Draft draft, TurboSquid client, CancellationToken cancellationToken)
        {
            try
            {
                _logger.Trace("Initializing session.");
                // Session is initialized either as newly created draft or as already created draft (with ID) being edited so it's the draft functionality as well thus shall be moved there.
                var session = new PublishSession(draft, client, cancellationToken);
                _logger.Debug($"Session is initialized with {client.Credential.AuthenticityToken} authenticity token.");
                return session;
            }
            catch (Exception ex)
            { throw new Exception("Session initialization failed.", ex); }
        }

        PublishSession(
            TurboSquid._3DProduct.Draft draft,
            TurboSquid client,
            CancellationToken cancellationToken)
        {
            Draft = draft;
            Client = client;
            CancellationToken = cancellationToken;
        }

        internal async Task<PublishSession.Finished> StartAsync()
        {
            try
            {
                _logger.Trace("Starting 3D product assets upload and processing.");
#if ENABLE_PARALLELIZATION
                await Task.WhenAll(
                    UploadModelsAsync().ForEachAwaitWithCancellationAsync(
                        UploadModelMetadataAsync, CancellationToken),
                    UploadThumbnailsAsync(),
                    UploadTexturesAsync());
#else
                await UploadModelsAsync().ForEachAwaitWithCancellationAsync(
                    UploadModelMetadataAsync, CancellationToken);
                await UploadThumbnailsAsync();
                await UploadTexturesAsync();
#endif
                _logger.Trace("3D product assets have been uploaded.");
                return new PublishSession.Finished(this);
            }
            catch (Exception ex)
            { throw new Exception("3D product assets upload and processing failed.", ex); }


            async IAsyncEnumerable<TurboSquidProcessed3DModel> UploadModelsAsync()
            {
                _logger.Trace("Starting 3D models upload and processing.");
                var modelsProcessing = new List<TurboSquid3DProductAssetProcessing.Task_<_3DModel<TurboSquid3DModelMetadata>>>();
#if ENABLE_PARALLELIZATION
                await Parallel.ForEachAsync(Draft.LocalProduct._3DModels.Where(_ => _ is not ITurboSquidProcessed3DProductAsset),
                    async (_3DModel, _) =>
                    {
                        string uploadKey = await UploadAssetAsync(_3DModel.Archived);
                        modelsProcessing.Add(await TurboSquid3DProductAssetProcessing.Task_<_3DModel<TurboSquid3DModelMetadata>>.RunAsync(_3DModel, uploadKey, this));
                    });
#else
                foreach (var _3DModel in Draft.LocalProduct._3DModels.Where(_ => _ is not ITurboSquidProcessed3DProductAsset))
                {
                    string uploadKey = await UploadAssetAsyncAt(await _3DModel.Archived);
                    modelsProcessing.Add(await TurboSquid3DProductAssetProcessing.Task_<_3DModel<TurboSquid3DModelMetadata>>.RunAsync(_3DModel, uploadKey, this));
                }
#endif
                var processedModels = (await TurboSquid3DProductAssetProcessing.Task_<_3DModel<TurboSquid3DModelMetadata>>.WhenAll(modelsProcessing)).Cast<TurboSquidProcessed3DModel>();
                Draft.LocalProduct.Synchronize(processedModels);
                _logger.Trace("3D models have been uploaded and processed.");
                foreach (var processedModel in processedModels.Concat(Draft.Edited3DModels))
                    yield return processedModel;
            }

            // _3DModel itself either exists or doesn't exist and thus modified (deleted on the turbosquid servers) only if it's not present locally but still exists remotely (i.e. has been deserialized to `Product`).
            async Task UploadModelMetadataAsync(TurboSquidProcessed3DModel processedModel, CancellationToken cancellationToken)
            {
                try
                {
                    _logger.Trace($"Uploading {processedModel.Name} metadata");
                    await Client.SendAsync(
                        new HttpRequestMessage(
                            HttpMethod.Patch,

                            $"turbosquid/products/{Draft.ID}/product_files/{processedModel.FileId}")
                        { Content = MetadataForm() },
                        cancellationToken);
                    _logger.Trace($"Metadata for {processedModel.Name} has been uploaded.");
                }
                catch (Exception ex)
                { throw new HttpRequestException($"{processedModel.Name} metadata upload failed.", ex); }


                StringContent MetadataForm()
                {
                    using var archived3DModel = File.OpenRead(processedModel.Archived);
                    // Explicit conversions of numbers to strings are required (except `size`).
                    var metadataForm = new JObject(
                        new JProperty("authenticity_token", Client.Credential.AuthenticityToken),
                        new JProperty("draft_id", Draft.ID.ToString()),
                        new JProperty("file_format", processedModel.Metadata.FileFormat),
                        new JProperty("format_version", processedModel.Metadata.FormatVersion.ToString()),
                        new JProperty("id", processedModel.FileId.ToString()),
                        new JProperty("is_native", processedModel.Metadata.IsNative),
                        new JProperty("name", Path.GetFileName(archived3DModel.Name)),
                        new JProperty("product_id", Draft.LocalProduct.ID.ToString()),
                        new JProperty("size", archived3DModel.Length));
                    if (processedModel.Metadata.Renderer is string renderer)
                    {
                        metadataForm.Add("renderer", renderer);
                        if (processedModel.Metadata.RendererVersion is double version)
                            metadataForm.Add("renderer_version", version.ToString());
                    }

                    return metadataForm.ToJsonContent();
                }
            }

            async Task<IEnumerable<TurboSquidProcessed3DProductThumbnail>> UploadThumbnailsAsync()
            {
                _logger.Trace("Starting 3D product thumbnails upload and processing.");
                var thumbnailsProcessing = new List<TurboSquid3DProductAssetProcessing.Task_<_3DProductThumbnail>>();
                foreach (var thumbnail in Draft.LocalProduct.Thumbnails.Where(_ => _ is not ITurboSquidProcessed3DProductAsset))
                {
                    string uploadKey = await UploadAssetAsync(thumbnail.FilePath);
                    thumbnailsProcessing.Add(await TurboSquid3DProductAssetProcessing.Task_<_3DProductThumbnail>.RunAsync(thumbnail, uploadKey, this));
                };
                var processedThumbnails = (await TurboSquid3DProductAssetProcessing.Task_<_3DProductThumbnail>.WhenAll(thumbnailsProcessing)).Cast<TurboSquidProcessed3DProductThumbnail>();
                foreach (var processedThumbnail in processedThumbnails)
                    Draft.LocalProduct.Synchronize(processedThumbnail);
                _logger.Trace("3D product thumbnails have been uploaded and processed.");
                return processedThumbnails;
            }

            async Task<IEnumerable<TurboSquidProcessed3DProductTexture>?> UploadTexturesAsync()
            {
                if (Draft.LocalProduct.Textures is null) return null;
                _logger.Trace("Starting 3D product textures upload and processing.");
                var texturesProcessing = new List<TurboSquid3DProductAssetProcessing.Task_<_3DProductDS._3DProduct.Texture_>>();
                foreach (var texture in Draft.LocalProduct.Textures.EnumerateFiles())
                {
                    string uploadKey = await UploadAssetAsync(texture.Path);
                    texturesProcessing.Add(await TurboSquid3DProductAssetProcessing.Task_<_3DProductDS._3DProduct.Texture_>.RunAsync(texture, uploadKey, this));
                }
                var processedTextures = (await TurboSquid3DProductAssetProcessing.Task_<_3DProductDS._3DProduct.Texture_>.WhenAll(texturesProcessing)).Cast<TurboSquidProcessed3DProductTexture>();
                _logger.Trace("3D product textures have been uploaded and processed.");
                return processedTextures;
            }



            async Task<string> UploadAssetAsync(string assetPath)
            {
                try
                {
                    _logger.Trace($"Uploading asset at {assetPath}.");
                    using var asset = File.OpenRead(assetPath);
                    string uploadKey = await (await AssetUploadRequest.CreateAsyncFor(asset, this)).SendAsync();
                    _logger.Trace($"Asset at {assetPath} has been uploaded.");
                    return uploadKey;
                }
                catch (Exception ex)
                { throw new HttpRequestException($"{assetPath} asset upload failed.", ex); }
            }
        }


        internal class Finished
        {
            internal Finished(PublishSession session)
            { _session = session; }
            readonly PublishSession _session;

            internal async Task FinalizeAsync()
            {
                try
                {
                    _logger.Trace($"Sending {_session.Draft.LocalProduct.Metadata.Title} 3D product publish request.");
                    var productPublishRequest = new HttpRequestMessage(
                        HttpMethod.Patch,

                        $"turbosquid/products/{_session.Draft.LocalProduct.ID}")
                    { Content = ProductForm() };
                    productPublishRequest.Headers.Add(HeaderNames.Origin, Origin.OriginalString);
                    productPublishRequest.Headers.Add(HeaderNames.Accept, MediaTypeNames.Application.Json);
                    await _session.Client.SendAsync(productPublishRequest, _session.CancellationToken);
                    _logger.Trace($"{_session.Draft.LocalProduct.Metadata.Title} 3D product publish request has been sent.");

                    //// Edit with Draft.ID will work only if it was uploaded as Draft and not published as Product and Product ID is unknow at this point.
                    //var remote = _3DProduct.Remote.Parse(await _session.Client.EditAsync(_session.Draft.ID, isDraft: true, _session.CancellationToken));
                    if (_session.Draft.LocalProduct.Metadata.Status is RFProduct._3D.Status.online)
                    {
                        await Task.Delay(5000);
                        _session.Draft.LocalProduct.ID = await RequestPublishedProductIdAsync();
                    }
                    else _session.Draft.LocalProduct.ID = _session.Draft.ID;
                    TurboSquid._3DProduct.Metadata__.File.For(_session.Draft.LocalProduct).Write(_session.Draft.LocalProduct);
                }
                catch (Exception ex)
                { throw new HttpRequestException($"{_session.Draft.LocalProduct.Metadata.Title} 3D product publish request failed.", ex); }


                StringContent ProductForm()
                {
                    var productForm = new JObject(
                        new JProperty("authenticity_token", _session.Client.Credential.AuthenticityToken),
                        new JProperty("turbosquid_product_form", _session.Draft.LocalProduct.Metadata.ToProductForm(_session.Draft.ID)),
                        new JProperty("previews", new JObject(
                            _session.Draft.LocalProduct.Thumbnails.OfType<TurboSquidProcessed3DProductThumbnail>().Select(_ => new JProperty(
                                _.FileId.ToString(), JObject.FromObject(new
                                {
                                    id = _.FileId.ToString(),
                                    image_type = _.Type.ToString()
                                }))
                            ))),
                        new JProperty("feature_ids", _session.Draft.LocalProduct.Metadata.Features.Values.ToArray()),
                        new JProperty("missing_brand", JObject.FromObject(new
                        {
                            name = string.Empty,
                            website = string.Empty
                        })));
                    if (_session.Draft.LocalProduct.Metadata.Status is RFProduct._3D.Status.online)
                        productForm.Add("publish", string.Empty);
                    return productForm.ToJsonContent();
                }

                async Task<long> RequestPublishedProductIdAsync()
                {
                    const string ID = "turbosquid_id";
                    try
                    {
                        if (JObject.Parse(await _session.Client.GetStringAsync("/turbosquid/products.json?page=1", _session.CancellationToken))["data"] is JArray publishedProducts)
                            if (publishedProducts.FirstOrDefault(_ => (string)_["name"]! == _session.Draft.LocalProduct.Metadata.Title) is JToken publishedProduct)
                                if (publishedProduct[ID]?.Value<long>() is long id)
                                { _logger.Trace($"{_session.Draft.LocalProduct.Metadata.Title} 3D product ID is obtained."); return id; }
                                else throw new MissingFieldException("PublishedProduct", ID);
                            else throw new Exception($"{_session.Draft.LocalProduct.Metadata.Title} wasn't found among published 3D products.");
                        else throw new InvalidDataException("Published products request failed.");
                    }
                    catch (Exception ex)
                    { throw new HttpRequestException($"{_session.Draft.LocalProduct.Metadata.Title} 3D product ID request failed.", ex); }
                }
            }
        }
    }
}
