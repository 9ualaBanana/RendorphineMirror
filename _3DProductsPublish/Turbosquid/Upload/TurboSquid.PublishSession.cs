#define ENABLE_PARALLELIZATION

using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.CGTrader.Upload;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Upload.Processing;
using _3DProductsPublish.Turbosquid.Upload.Requests;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;

namespace _3DProductsPublish.Turbosquid.Upload;

public partial class TurboSquid
{
    internal class PublishSession
    {
        static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        internal readonly _3DProductDraft<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata> Draft;
        internal readonly TurboSquidAwsUploadCredentials AwsCredential;
        internal readonly TurboSquid Client;
        internal readonly CancellationToken CancellationToken;

        internal static async Task<PublishSession> InitializeAsync(_3DProductDraft<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata> draft, TurboSquid client, CancellationToken cancellationToken)
        {
            try
            {
                _logger.Trace("Initializing session.");
                // Session is initialized either as newly created draft or as already created draft (with ID) being edited so it's the draft functionality as well thus shall be moved there.
                var session = new PublishSession(draft, await RequestAwsStorageCredentialsAsync(), client, cancellationToken);
                _logger.Debug($"Session is initialized with {client.Credential.AuthenticityToken} authenticity token.");
                return session;
            }
            catch (Exception ex)
            { throw new Exception("Session initialization failed.", ex); }


            async Task<TurboSquidAwsUploadCredentials> RequestAwsStorageCredentialsAsync()
            {
                var authenticity_token = client.Credential.AuthenticityToken;
                try
                {
                    var credentials = TurboSquidAwsUploadCredentials.Parse(await
                        (await client.PostAsJsonAsync("turbosquid/uploads//credentials", new { authenticity_token }, cancellationToken))
                        .EnsureSuccessStatusCode()
                        .Content.ReadAsStringAsync(cancellationToken));
                    _logger.Trace($"AWS credential for {authenticity_token} session has been obtained.");
                    return credentials;
                }
                catch (Exception ex)
                { throw new Exception($"AWS credential request for {authenticity_token} session failed.", ex); }
            }
        }

        PublishSession(
            _3DProductDraft<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata> draft,
            TurboSquidAwsUploadCredentials awsCredential,
            TurboSquid client,
            CancellationToken cancellationToken)
        {
            Draft = draft;
            AwsCredential = awsCredential;
            Client = client;
            CancellationToken = cancellationToken;
        }

        internal async Task<PublishSession.Finished> StartAsync()
        {
            var processedThumbnails = new List<ITurboSquidProcessed3DProductAsset<_3DProductThumbnail>>();
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
                var finishedSession = new PublishSession.Finished(this, processedThumbnails);
                _logger.Trace("3D product assets have been uploaded.");
                return finishedSession;
            }
            catch (Exception ex)
            { throw new Exception("3D product assets upload and processing failed.", ex); }


            async IAsyncEnumerable<ITurboSquidProcessed3DProductAsset<_3DModel<TurboSquid3DModelMetadata>>> UploadModelsAsync()
            {
                _logger.Trace("Starting 3D models upload and processing.");
                var modelsProcessing = new List<TurboSquid3DProductAssetProcessing.Task_<_3DModel<TurboSquid3DModelMetadata>>>();
#if ENABLE_PARALLELIZATION
                await Parallel.ForEachAsync(Draft.Product._3DModels,
                    async (_3DModel, _) =>
                    {
                        string uploadKey = await UploadAssetAsyncAt(await _3DModel.Archive());
                        modelsProcessing.Add(
                            await TurboSquid3DProductAssetProcessing.Task_<_3DModel<TurboSquid3DModelMetadata>>.RunAsync(_3DModel, uploadKey, this)
                            );
                    });
#else
                foreach (var _3DModel in Draft._Product._3DModels)
                {
                    string uploadKey = await UploadAssetAsyncAt(await _3DModel.Archive());
                    modelsProcessing.Add(
                        await TurboSquid3DProductAssetProcessing.Task_<_3DModel<TurboSquid3DModelMetadata>>.RunAsync(_3DModel, uploadKey, this)
                        );
                }
#endif
                foreach (var processedModel in await TurboSquid3DProductAssetProcessing.Task_<_3DModel<TurboSquid3DModelMetadata>>.WhenAll(modelsProcessing))
                    yield return processedModel;
                _logger.Trace("3D models have been uploaded and processed.");
            }

            async Task UploadModelMetadataAsync(
                ITurboSquidProcessed3DProductAsset<_3DModel<TurboSquid3DModelMetadata>> processedModel,
                CancellationToken cancellationToken)
            {
                try
                {
                    _logger.Trace($"Uploading {processedModel.Asset.Name} metadata");
                    await Client.SendAsync(
                        new HttpRequestMessage(
                            HttpMethod.Patch,

                            $"turbosquid/products/{Draft.ID}/product_files/{processedModel.FileId}")
                        { Content = MetadataForm() },
                        cancellationToken);
                    _logger.Trace($"Metadata for {processedModel.Asset.Name} has been uploaded.");
                }
                catch (Exception ex)
                { throw new HttpRequestException($"{processedModel.Asset.Name} metadata upload failed.", ex); }


                StringContent MetadataForm()
                {
                    using var archived3DModel = File.OpenRead(processedModel.Asset.Archive().Result);
                    // Explicit conversions of numbers to strings are required.
                    var metadataForm = new JObject(
                        new JProperty("authenticity_token", Client.Credential.AuthenticityToken),
                        new JProperty("draft_id", Draft.ID),
                        new JProperty("file_format", processedModel.Asset.Metadata.FileFormat),
                        new JProperty("format_version", processedModel.Asset.Metadata.FormatVersion.ToString()),
                        new JProperty("id", processedModel.FileId),
                        new JProperty("is_native", processedModel.Asset.Metadata.IsNative),
                        new JProperty("name", Path.GetFileName(archived3DModel.Name)),
                        new JProperty("product_id", Draft.Product.ID.ToString()),
                        new JProperty("size", archived3DModel.Length));
                    if (processedModel.Asset.Metadata.Renderer is string renderer)
                    {
                        metadataForm.Add("renderer", renderer);
                        if (processedModel.Asset.Metadata.RendererVersion is double version)
                            metadataForm.Add("renderer_version", version.ToString());
                    }

                    return metadataForm.ToJsonContent();
                }
            }

            async Task<List<ITurboSquidProcessed3DProductAsset<_3DProductThumbnail>>> UploadThumbnailsAsync()
            {
                _logger.Trace("Starting 3D product thumbnails upload and processing.");
                var thumbnailsProcessing = new List<TurboSquid3DProductAssetProcessing.Task_<_3DProductThumbnail>>();
                foreach (var thumbnail in Draft.Product.Thumbnails)
                {
                    string uploadKey = await UploadAssetAsyncAt(thumbnail.FilePath);
                    thumbnailsProcessing.Add(
                        await TurboSquid3DProductAssetProcessing.Task_<_3DProductThumbnail>.RunAsync(thumbnail, uploadKey, this)
                        );
                };
                processedThumbnails = await TurboSquid3DProductAssetProcessing.Task_<_3DProductThumbnail>.WhenAll(thumbnailsProcessing);
                _logger.Trace("3D product thumbnails have been uploaded and processed.");
                return processedThumbnails;
            }

            async Task<List<ITurboSquidProcessed3DProductAsset<_3DProduct.Texture_>>?> UploadTexturesAsync()
            {
                if (Draft.Product.Textures is null) return null;
                _logger.Trace("Starting 3D product textures upload and processing.");
                var texturesProcessing = new List<TurboSquid3DProductAssetProcessing.Task_<_3DProduct.Texture_>>();
                foreach (var texture in Draft.Product.Textures.EnumerateFiles())
                {
                    string uploadKey = await UploadAssetAsyncAt(texture.Path);
                    texturesProcessing.Add(
                        await TurboSquid3DProductAssetProcessing.Task_<_3DProduct.Texture_>.RunAsync(texture, uploadKey, this)
                        );
                }
                var processedTextures = await TurboSquid3DProductAssetProcessing.Task_<_3DProduct.Texture_>.WhenAll(texturesProcessing);
                _logger.Trace("3D product textures have been uploaded and processed.");
                return processedTextures;
            }



            async Task<string> UploadAssetAsyncAt(string assetPath)
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
            readonly PublishSession _session;
            readonly IEnumerable<ITurboSquidProcessed3DProductAsset<_3DProductThumbnail>> _processedThumbnails;

            internal Finished(PublishSession session, IEnumerable<ITurboSquidProcessed3DProductAsset<_3DProductThumbnail>> processedThumbnails)
            {
                _session = session;
                _processedThumbnails = processedThumbnails;
            }

            internal async Task FinalizeAsync()
            {
                try
                {
                    _logger.Trace($"Sending {_session.Draft.Product.Metadata.Title} 3D product publish request.");
                    var productPublishRequest = new HttpRequestMessage(
                        HttpMethod.Patch,

                        $"turbosquid/products/{_session.Draft.Product.ID}")
                    { Content = ProductForm() };
                    productPublishRequest.Headers.Add(HeaderNames.Origin, Origin.OriginalString);
                    productPublishRequest.Headers.Add(HeaderNames.Accept, MediaTypeNames.Application.Json);
                    await _session.Client.SendAsync(productPublishRequest, _session.CancellationToken);
                    _logger.Trace($"{_session.Draft.Product.Metadata.Title} 3D product publish request has been sent.");
                    _session.Draft.Product.ID = await RequestPublishedProductIdAsync();
                }
                catch (Exception ex)
                { throw new HttpRequestException($"{_session.Draft.Product.Metadata.Title} 3D product publish request failed.", ex); }


                StringContent ProductForm()
                    => new JObject(
                        new JProperty("authenticity_token", _session.Client.Credential.AuthenticityToken),
                        new JProperty("turbosquid_product_form", _session.Draft.Product.Metadata.ToProductForm(_session.Draft.ID)),
                        new JProperty("previews", new JObject(
                            _processedThumbnails.Select(_ => new JProperty(
                                _.FileId, JObject.FromObject(new
                                {
                                    id = _.FileId,
                                    image_type = _.Type().ToString()
                                }))
                            ))),
                        new JProperty("feature_ids", _session.Draft.Product.Metadata.Features.Values.ToArray()),
                        new JProperty("missing_brand", JObject.FromObject(new
                        {
                            name = string.Empty,
                            website = string.Empty
                        })),
                        new JProperty("publish", string.Empty))
                    .ToJsonContent();

                async Task<int> RequestPublishedProductIdAsync()
                {
                    const string ID = "turbosquid_id";
                    try
                    {
                        if (JObject.Parse(await _session.Client.GetStringAsync("/turbosquid/products.json?page=1", _session.CancellationToken))["data"] is JArray publishedProducts)
                            if (publishedProducts.FirstOrDefault(_ => (string)_["name"]! == _session.Draft.Product.Metadata.Title) is JToken publishedProduct)
                                if (publishedProduct[ID]?.Value<int>() is int id)
                                { _logger.Trace($"{_session.Draft.Product.Metadata.Title} 3D product ID is obtained."); return id; }
                                else throw new MissingFieldException("PublishedProduct", ID);
                            else throw new Exception($"{_session.Draft.Product.Metadata.Title} wasn't found among published 3D products.");
                        else throw new InvalidDataException("Published products request failed.");
                    }
                    catch (Exception ex)
                    { throw new HttpRequestException($"{_session.Draft.Product.Metadata.Title} 3D product ID request failed.", ex); }
                }
            }
        }


        internal Uri UploadEndpointFor(FileStream asset, string unixTimestamp) =>
            new(new Uri(new($"https://{AwsCredential.Bucket}.s3.amazonaws.com/{AwsCredential.KeyPrefix}"), unixTimestamp + '/'), Path.GetFileName(asset.Name));
    }
}
