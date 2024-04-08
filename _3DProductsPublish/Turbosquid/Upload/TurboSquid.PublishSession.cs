#define ENABLE_PARALLELIZATION

using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid.Upload.Processing;
using _3DProductsPublish.Turbosquid.Upload.Requests;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.RegularExpressions;
using static MarkTM.RFProduct.RFProduct._3D;

namespace _3DProductsPublish.Turbosquid.Upload;

public partial class TurboSquid
{
    // TODO: Refactor.
    internal partial class PublishSession
    {
        static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        internal _3DProduct _3DProduct { get; }
        internal TurboSquidAwsSession AWS { get; init; }
        SynchronizationContext_ SynchronizationContext { get; }
        internal TurboSquid Client { get; }
        internal readonly CancellationToken CancellationToken;

        internal static async Task<PublishSession> InitializeAsync(_3DProduct _3DProduct, TurboSquid client, CancellationToken cancellationToken)
        {
            try
            {
                _logger.Trace("Initializing session.");
                var drafted3DProduct = await CreateDraftAsync();
                var aws = await RequestAwsStorageCredentialAsync();
                var session = new PublishSession(drafted3DProduct, aws, client, cancellationToken);
                _logger.Debug($"Session is initialized with {client.Credential.AuthenticityToken} authenticity token.");
                return session;
            }
            catch (Exception ex)
            { throw new Exception("Session initialization failed.", ex); }


            // Body part under <script> tag in the responses contains all the necessary information/references related to the product and *its manipulation*.
            // Refactor the code to use those.
            /// <remarks>
            /// Prioritazes <see cref="_3DProduct.Tracker_.Data_.DraftID"/> over <see cref="_3DProduct.Tracker_.Data_.ID"/>.
            /// </remarks>
            async Task<_3DProduct> CreateDraftAsync()
            {
                try
                {
                    _3DProduct.Remote remote = await client.CreateOrRequestRemoteAsync(_3DProduct, cancellationToken);
                    if ((_3DProduct.Tracker.Data.DraftID = remote.draft_id ?? 0) is 0)
                        _3DProduct.Tracker.Data.DraftID = await CreateDraftAsync();
                    _3DProduct.Tracker.Data.Status = Status.draft;
                    _3DProduct.Tracker.Write();

                    _logger.Trace($"3D product draft with {_3DProduct.Tracker.Data.DraftID} ID has been created for {_3DProduct.Metadata.Title}.");
                    return _3DProduct;


                    // Returns the ID of the newly created or already existing draft for the given `_3DProduct.ID`.
                    async Task<long> CreateDraftAsync()
                        => JObject.Parse(await client.GetStringAsync($"turbosquid/products/{_3DProduct.Tracker.Data.ProductID}/create_draft", cancellationToken))["id"]!.Value<long>()!;
                }
                catch (Exception ex)
                { throw new Exception($"Failed to create a 3D product draft for {_3DProduct.Metadata.Title}.", ex); }
            }

            async Task<TurboSquidAwsSession> RequestAwsStorageCredentialAsync()
            {
                var authenticity_token = client.Credential.AuthenticityToken;
                try
                {
                    var awsCredential = TurboSquidAwsSession.Parse(await
                        (await client.PostAsJsonAsync("turbosquid/uploads//credentials", new { authenticity_token }, cancellationToken))
                        .EnsureSuccessStatusCode()
                        .Content.ReadAsStringAsync(cancellationToken));
                    _logger.Trace($"AWS credential for {authenticity_token} session has been obtained.");
                    return awsCredential;
                }
                catch (Exception ex)
                { throw new Exception($"AWS credential request for {authenticity_token} session failed.", ex); }
            }
        }

        async Task DeleteDraftAsync()
            => await Client.SendAsync(new(HttpMethod.Delete, $"turbosquid/products/{_3DProduct.Tracker.Data.ProductID}/delete_draft")
            { Content = JsonContent.Create(new { authenticity_token = Client.Credential.AuthenticityToken }) },
            CancellationToken);

        PublishSession(
            _3DProduct _3DProduct,
            TurboSquidAwsSession aws,
            TurboSquid client,
            CancellationToken cancellationToken)
        {
            (this._3DProduct = _3DProduct).Synchronize();
            AWS = aws;
            Client = client;
            CancellationToken = cancellationToken;
            SynchronizationContext = new(this);
        }

        internal async Task<PublishSession.Finished> StartAsync()
        {
            try
            {
                _logger.Trace("Starting 3D product assets upload and processing.");
#if ENABLE_PARALLELIZATION
                await Task.WhenAll(
                    UploadModelsAsync().ForEachAwaitAsync(UploadModelMetadataAsync),
                    UploadThumbnailsAsync(),
                    UploadTexturesAsync());
#else
                await UploadModelsAsync().ForEachAwaitAsync(UploadModelMetadataAsync);
                await UploadThumbnailsAsync();
                await UploadTexturesAsync();
#endif
                _logger.Trace("3D product assets have been uploaded.");
                await OrderThumbnailsAsync();
                return new PublishSession.Finished(this);
            }
            catch (Exception ex)
            { throw new Exception("3D product assets upload and processing failed.", ex); }


            async IAsyncEnumerable<TurboSquidProcessed3DModel> UploadModelsAsync()
            {
                _logger.Trace("Starting 3D models upload and processing.");
                await SynchronizationContext.DesynchronizeOutdatedAssetsAsync<TurboSquidProcessed3DModel>();
                var processedModels = await UploadModelsAsyncCore();
                SynchronizationContext.Synchronize(processedModels);
                _logger.Trace("3D models have been uploaded and processed.");
                foreach (var processedModel in processedModels)
                    yield return processedModel;


                async Task<IEnumerable<TurboSquidProcessed3DModel>> UploadModelsAsyncCore()
                {
                    var modelsProcessing = new List<TurboSquid3DProductAssetProcessing.Task_<_3DModel>>();
#if ENABLE_PARALLELIZATION
                    await Parallel.ForEachAsync(_3DProduct._3DModels.Where(_ => _ is not ITurboSquidProcessed3DProductAsset),
                        async (_3DModel, _) =>
                        {
                            string uploadKey = await UploadAssetAsync(_3DModel.Archived);
                            modelsProcessing.Add(await TurboSquid3DProductAssetProcessing.Task_<_3DModel>.RunAsync(_3DModel, uploadKey, this));
                        });
#else
                    foreach (var _3DModel in Draft.LocalProduct._3DModels.Where(_ => _ is not ITurboSquidProcessed3DProductAsset))
                    {
                        string uploadKey = await UploadAssetAsyncAt(_3DModel.Archived);
                        modelsProcessing.Add(await TurboSquid3DProductAssetProcessing.Task_<_3DModel<TurboSquid3DModelMetadata>>.RunAsync(_3DModel, uploadKey, this));
                    }
#endif
                    return (await TurboSquid3DProductAssetProcessing.Task_<_3DModel>.WhenAll(modelsProcessing)).Cast<TurboSquidProcessed3DModel>();
                }
            }

            // _3DModel itself either exists or doesn't exist and thus modified (deleted on the turbosquid servers) only if it's not present locally but still exists remotely (i.e. has been deserialized to `Product`).
            async Task UploadModelMetadataAsync(TurboSquidProcessed3DModel processedModel)
            {
                try
                {
                    _logger.Trace($"Uploading {processedModel.Name()} metadata");
                    await Client.SendAsync(
                        new HttpRequestMessage(
                            HttpMethod.Patch,

                            $"turbosquid/products/{_3DProduct.Tracker.Data.DraftID}/product_files/{processedModel.FileId}")
                        { Content = MetadataForm() },
                        CancellationToken);
                    _logger.Trace($"Metadata for {processedModel.Name()} has been uploaded.");
                }
                catch (Exception ex)
                { throw new HttpRequestException($"{processedModel.Name()} metadata upload failed.", ex); }


                StringContent MetadataForm()
                {
                    using var archived3DModel = File.OpenRead(processedModel.Archived);
                    var metadata = _3DProduct.Tracker.Model(processedModel)?._ ?? throw new InvalidOperationException();
                    // Explicit conversions of numbers to strings are required (except `size`).
                    var metadataForm = new JObject(
                        new JProperty("authenticity_token", Client.Credential.AuthenticityToken),
                        new JProperty("draft_id", _3DProduct.Tracker.Data.DraftID.ToString()),
                        new JProperty("file_format", metadata.FileFormat),
                        new JProperty("format_version", metadata.FormatVersion.ToString()),
                        new JProperty("id", processedModel.FileId.ToString()),
                        new JProperty("is_native", metadata.IsNative),
                        new JProperty("name", Path.GetFileName(archived3DModel.Name)),
                        new JProperty("product_id", _3DProduct.Tracker.Data.ProductID.ToString()),
                        new JProperty("size", archived3DModel.Length));
                    if (metadata.Renderer is string renderer)
                    {
                        metadataForm.Add("renderer", renderer);
                        if (metadata.RendererVersion is double version)
                            metadataForm.Add("renderer_version", version.ToString());
                    }

                    return metadataForm.ToJsonContent();
                }
            }

            async Task<IEnumerable<TurboSquidProcessed3DProductThumbnail>> UploadThumbnailsAsync()
            {
                _logger.Trace("Starting 3D product thumbnails upload and processing.");
                await SynchronizationContext.DesynchronizeOutdatedAssetsAsync<TurboSquidProcessed3DProductThumbnail>();
                var processedThumbnails = await UploadThumbnailsAsyncCore();
                SynchronizationContext.Synchronize(processedThumbnails);
                _logger.Trace("3D product thumbnails have been uploaded and processed.");
                return processedThumbnails;


                async Task<IEnumerable<TurboSquidProcessed3DProductThumbnail>> UploadThumbnailsAsyncCore()
                {
                    var thumbnailsProcessing = new List<TurboSquid3DProductAssetProcessing.Task_<_3DProductThumbnail>>();
                    foreach (var thumbnail in _3DProduct.Thumbnails.Where(_ => _ is not ITurboSquidProcessed3DProductAsset))
                    {
                        string uploadKey = await UploadAssetAsync(thumbnail.Path);
                        thumbnailsProcessing.Add(await TurboSquid3DProductAssetProcessing.Task_<_3DProductThumbnail>.RunAsync(thumbnail, uploadKey, this));
                    };
                    return (await TurboSquid3DProductAssetProcessing.Task_<_3DProductThumbnail>.WhenAll(thumbnailsProcessing)).Cast<TurboSquidProcessed3DProductThumbnail>();
                }
            }

            async Task<IEnumerable<TurboSquidProcessed3DProductTextures>> UploadTexturesAsync()
            {
                _logger.Trace("Starting 3D product textures upload and processing.");
                var texturesProcessing = new List<TurboSquid3DProductAssetProcessing.Task_<_3DProductDS._3DProduct.Textures_>>();
                foreach (var textures in _3DProduct.Textures.Where(_ => _ is not ITurboSquidProcessed3DProductAsset))
                {
                    string uploadKey = await UploadAssetAsync(textures.Path);
                    texturesProcessing.Add(await TurboSquid3DProductAssetProcessing.Task_<_3DProductDS._3DProduct.Textures_>.RunAsync(textures, uploadKey, this));
                }
                var processedTextures = (await TurboSquid3DProductAssetProcessing.Task_<_3DProductDS._3DProduct.Textures_>.WhenAll(texturesProcessing)).Cast<TurboSquidProcessed3DProductTextures>();
                _logger.Trace("3D product textures have been uploaded and processed.");
                return processedTextures;
            }
        }

        async Task<string> UploadAssetAsync(string assetPath)
        {
            try
            {
                _logger.Debug($"Uploading asset at {assetPath}.");
                using var asset = File.OpenRead(assetPath);
                string uploadKey = await (await AssetUploadRequest.CreateAsyncFor(asset, this)).SendAsync();
                _logger.Info($"Asset at {assetPath} has been uploaded.");
                return uploadKey;
            }
            catch (Exception ex)
            { throw new HttpRequestException($"{assetPath} asset upload failed.", ex); }
        }

        internal async Task OrderThumbnailsAsync()
        {
            var remotePreviews = JObject.Parse(await (await SaveDraftAsync(false)).EnsureSuccessStatusCode().Content.ReadAsStringAsync(CancellationToken))
                .SelectToken("product.previews").ToArray()
                .Select(_ => _.ToObject<_3DProduct.Remote.DraftPreview>());
            var orderedThumbnails = remotePreviews
                .OrderBy(_ =>
                {
                    if (NumericallyOrdered().IsMatch(_.attributes.filename))
                        return 1;
                    else if (_.attributes.filename.EndsWith("_vp") || _.attributes.filename.EndsWith("_wire"))
                        return 2;
                    else return 3;
                })
                .Select((_, index) => _.ToOrdered(index));
            (await Client.PutAsJsonAsync($"/turbosquid/products/{_3DProduct.Tracker.Data.DraftID}/thumbnails/order_thumbnails",
                new
                {
                    authenticity_token = Client.Credential.AuthenticityToken,
                    draft_id = _3DProduct.Tracker.Data.DraftID,
                    data = orderedThumbnails
                }, CancellationToken))
                .EnsureSuccessStatusCode();
        }
        [GeneratedRegex(@"^\d+_")]
        private static partial Regex NumericallyOrdered();

        internal async Task<HttpResponseMessage> SaveDraftAsync(bool publish)
        {
            try
            {
                _logger.Trace($"Sending {_3DProduct.Metadata.Title} ({_3DProduct.Tracker.Data.ProductID}) 3D product form.");
                var request = new HttpRequestMessage(
                    HttpMethod.Patch,

                    $"turbosquid/products/{_3DProduct.Tracker.Data.ProductID}")
                { Content = ProductForm() };
                request.Headers.Add(HeaderNames.Origin, Origin.OriginalString);
                request.Headers.Add(HeaderNames.Accept, MediaTypeNames.Application.Json);

                var response = await Client.SendAsync(request, CancellationToken);
                _logger.Trace($"{_3DProduct.Metadata.Title} 3D product form request has been sent.");
                return response;
            }
            catch (Exception ex)
            { throw new HttpRequestException($"{_3DProduct.Metadata.Title} 3D product publish request failed.", ex); }


            StringContent ProductForm()
            {
                var productForm = new JObject(
                    new JProperty("authenticity_token", Client.Credential.AuthenticityToken),
                    new JProperty("turbosquid_product_form", _3DProduct.Metadata.ToProductForm(_3DProduct)),
                    new JProperty("previews", new JObject(
                        _3DProduct.Thumbnails.OfType<TurboSquidProcessed3DProductThumbnail>().Select(_ => new JProperty(
                            _.FileId.ToString(), JObject.FromObject(new
                            {
                                id = _.FileId.ToString(),
                                image_type = _.Type.ToString()
                            }))
                        ))),
                    new JProperty("feature_ids", _3DProduct.Metadata.Features.Values.ToArray()),
                    new JProperty("missing_brand", JObject.FromObject(new
                    {
                        name = string.Empty,
                        website = string.Empty
                    })));
                if (publish)
                    productForm.Add("publish", string.Empty);
                return productForm.ToJsonContent();
            }
        }

        async Task<_3DProduct.Remote> CreateOrRequestRemoteAsync()
            => await Client.CreateOrRequestRemoteAsync(_3DProduct, CancellationToken);


        internal class Finished
        {
            internal Finished(PublishSession session)
            { _session = session; }
            readonly PublishSession _session;

            internal async Task FinalizeAsync()
            {
                await _session.SaveDraftAsync(publish: _session._3DProduct.Metadata.Status is Status.online);
                if (_session._3DProduct.Metadata.Status is Status.online)
                {
                    if (_session._3DProduct.Tracker.Data.ProductID is 0)
                    {
                        await Task.Delay(5000);
                        _session._3DProduct.Tracker.Data.ProductID = await RequestPublishedProductIdAsync();
                    }
                    _session._3DProduct.Tracker.Data.DraftID = 0;
                }
                _session._3DProduct.Tracker.Data.Status = (await _session.CreateOrRequestRemoteAsync()).status;
                _session._3DProduct.Tracker.Write();


                async Task<long> RequestPublishedProductIdAsync()
                {
                    const string ID = "turbosquid_id";
                    try
                    {
                        if (JObject.Parse(await _session.Client.GetStringAsync("/turbosquid/products.json?page=1", _session.CancellationToken))["data"] is JArray publishedProducts)
                        {
                            if (publishedProducts.FirstOrDefault(_ => (string)_["name"]! == _session._3DProduct.Metadata.Title) is JToken publishedProduct)
                                if (publishedProduct[ID]?.Value<long>() is long id)
                                { _logger.Trace($"{_session._3DProduct.Metadata.Title} 3D product ID is obtained."); return id; }
                                else throw new MissingFieldException("PublishedProduct", ID);
                            else throw new Exception($"{_session._3DProduct.Metadata.Title} wasn't found among published 3D products.");
                        }
                        else throw new InvalidDataException("Published products request failed.");
                    }
                    catch (Exception ex)
                    { throw new HttpRequestException($"{_session._3DProduct.Metadata.Title} 3D product ID request failed.", ex); }
                }
            }
        }
    }
}
