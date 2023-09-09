using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.CGTrader.Upload;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Network.Authenticity;
using _3DProductsPublish.Turbosquid.Upload.Processing;
using _3DProductsPublish.Turbosquid.Upload.Requests;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;

namespace _3DProductsPublish.Turbosquid.Upload;

internal partial class TurboSquid
{
    internal class PublishSession
    {
        static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        internal readonly _3DProductDraft<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata> Draft;
        internal readonly TurboSquidNetworkCredential Credential;
        internal readonly TurboSquidAwsUploadCredentials AwsCredential;
        internal readonly TurboSquid Client;
        internal readonly CancellationToken CancellationToken;

        internal static async Task<PublishSession> InitializeAsync(_3DProduct<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata> _3DProduct, TurboSquid client, CancellationToken cancellationToken)
        {
            try
            {
                _logger.Trace("Initializing session.");
                var authenticityToken = await RequestSessionAuthenticityTokenAsync();
                var session = new PublishSession(
                    new _3DProductDraft<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata>(_3DProduct, await CreateNewProductDraftAsync()),
                    client.Credential.WithUpdated(authenticityToken),
                    await RequestAwsUploadCredentialsAsync(authenticityToken), client, cancellationToken);
                _logger.Debug($"Session is initialized with {authenticityToken} authenticity token.");
                return session;
            }
            catch (Exception ex)
            { throw new Exception("Session initialization failed.", ex); }


            async Task<string> RequestSessionAuthenticityTokenAsync()
            {
                try
                {
                    var authenticityToken = CsrfToken._ParseFromMetaTag(await client.GetStringAsync("turbosquid/products/new", cancellationToken));
                    _logger.Trace("Authenticity token has been obtained.");
                    return authenticityToken;
                }
                catch (Exception ex)
                { throw new Exception("Authenticity token request failed.", ex); }
            }

            async Task<string> CreateNewProductDraftAsync()
            {
                try
                {
                    var draftId = JObject.Parse(await client.GetStringAsync("turbosquid/products/0/create_draft", cancellationToken))["id"]!.Value<string>()!;
                    _logger.Trace($"3D product draft with {draftId} ID has been created.");
                    return draftId;
                }
                catch (Exception ex)
                { throw new Exception("Failed to create 3D product draft.", ex); }
            }

            async Task<TurboSquidAwsUploadCredentials> RequestAwsUploadCredentialsAsync(string authenticity_token)
            {
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
            TurboSquidNetworkCredential credential,
            TurboSquidAwsUploadCredentials awsCredential,
            TurboSquid client,
            CancellationToken cancellationToken)
        {
            Draft = draft;
            Credential = credential;
            AwsCredential = awsCredential;
            Client = client;
            CancellationToken = cancellationToken;
        }

        internal async Task<PublishSession.Finished> StartAsync()
        {
            try
            {
                _logger.Trace("Starting 3D product assets upload and processing.");
                await UploadModelsAsync().ForEachAwaitWithCancellationAsync(UploadMetadataAsync, CancellationToken);
                var finishedSession = new PublishSession.Finished(this, await UploadThumbnailsAsync());
                _logger.Trace("3D product assets have been uploaded.");
                return finishedSession;
            }
            catch (Exception ex)
            { throw new Exception("3D product assets upload and processing failed.", ex); }

            async IAsyncEnumerable<ITurboSquidProcessed3DProductAsset<_3DModel<TurboSquid3DModelMetadata>>> UploadModelsAsync()
            {
                _logger.Trace("Starting 3D models upload and processing.");
                var modelsUpload = await Draft._Product._3DModels
                    .Select(async _3DModel =>
                    {
                        var archived3DModelPath = await _3DModel.ArchiveAsync(CancellationToken);
                        string uploadKey = await UploadAssetAsyncAt(archived3DModelPath);
                        return await TurboSquid3DProductAssetProcessing.Task_<_3DModel<TurboSquid3DModelMetadata>>
                            .RunAsync(_3DModel, uploadKey, this);
                    })
                    .RunAsync();
                foreach (var processedAsset in await TurboSquid3DProductAssetProcessing.Task_<_3DModel<TurboSquid3DModelMetadata>>.WhenAll(modelsUpload))
                    yield return processedAsset;
                _logger.Trace("3D models have been uploaded and processed.");
            }

            async Task<List<ITurboSquidProcessed3DProductAsset<_3DProductThumbnail>>> UploadThumbnailsAsync()
            {
                _logger.Trace("Starting 3D models thumbnails upload and processing.");
                var thumbnailsUpload = await Draft._Product.Thumbnails
                    .Select(async thumbnail =>
                    {
                        string uploadKey = await UploadAssetAsyncAt(thumbnail.FilePath);
                        return await TurboSquid3DProductAssetProcessing.Task_<_3DProductThumbnail>
                            .RunAsync(thumbnail, uploadKey, this);
                    })
                    .RunAsync();
                var processedThumbnails = await TurboSquid3DProductAssetProcessing.Task_<_3DProductThumbnail>.WhenAll(thumbnailsUpload);
                _logger.Trace("3D models thumbnails upload and processing failed.");
                return processedThumbnails;
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

            async Task UploadMetadataAsync(ITurboSquidProcessed3DProductAsset<_3DModel<TurboSquid3DModelMetadata>> processedModel, CancellationToken cancellationToken)
            {
                try
                {
                    _logger.Trace($"Uploading {processedModel.Asset.Name} metadata");
                    await Client.SendAsync(
                        new HttpRequestMessage(
                            HttpMethod.Patch,

                            $"turbosquid/products/{Draft._ID}/product_files/{processedModel.FileId}")
                        { Content = MetadataForm() },
                        cancellationToken);
                    _logger.Trace($"Metadata for {processedModel.Asset.Name} has been uploaded.");
                }
                catch (Exception ex)
                { throw new HttpRequestException($"{processedModel.Asset.Name} metadata upload failed.", ex); }


                StringContent MetadataForm()
                {
                    using var archived3DModel = File.OpenRead(processedModel.Asset.ArchiveAsync(CancellationToken.None).Result!);
                    // Explicit conversions of numbers to strings are required.
                    var metadataForm = new JObject(
                        new JProperty("authenticity_token", Credential._CsrfToken),
                        new JProperty("draft_id", Draft._ID),
                        new JProperty("file_format", processedModel.Asset.Metadata.FileFormat),
                        new JProperty("format_version", processedModel.Asset.Metadata.FormatVersion.ToString()),
                        new JProperty("id", processedModel.FileId),
                        new JProperty("is_native", processedModel.Asset.Metadata.IsNative),
                        new JProperty("name", Path.GetFileName(archived3DModel.Name)),
                        new JProperty("product_id", 0.ToString()),
                        new JProperty("size", archived3DModel.Length)
                        );
                    if (processedModel.Asset.Metadata.Renderer is string renderer)
                    {
                        metadataForm.Add("renderer", renderer);
                        if (processedModel.Asset.Metadata.RendererVersion is double version)
                            metadataForm.Add("renderer_version", version.ToString());
                    }

                    return metadataForm.ToJsonContent();
                }
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
                    _logger.Trace($"Sending {_session.Draft._Product.Metadata.Title} 3D product publish request.");
                    var productPublishRequest = new HttpRequestMessage(
                        HttpMethod.Patch,

                        "turbosquid/products/0")
                    { Content = ProductForm() };
                    productPublishRequest.Headers.Add(HeaderNames.Origin, Origin.OriginalString);
                    productPublishRequest.Headers.Add(HeaderNames.Accept, MediaTypeNames.Application.Json);
                    await _session.Client.SendAsync(productPublishRequest, _session.CancellationToken);
                    _logger.Trace($"{_session.Draft._Product.Metadata.Title} 3D product publish request has been sent.");
                }
                catch (Exception ex)
                { throw new HttpRequestException($"{_session.Draft._Product.Metadata.Title} 3D product publish request failed.", ex); }


                StringContent ProductForm()
                    => new JObject(
                    new JProperty("authenticity_token", _session.Credential._CsrfToken),
                    new JProperty("turbosquid_product_form", _session.Draft._Product.Metadata.ToProductForm(_session.Draft._ID)),
                    new JProperty("previews", new JObject(
                        _processedThumbnails.Select(_ => new JProperty(
                            _.FileId, JObject.FromObject(new
                            {
                                id = _.FileId,
                                image_type = _.Type().ToString()
                            }))
                        ))),
                    new JProperty("feature_ids", _session.Draft._Product.Metadata.Features.Values.ToArray()),
                    new JProperty("missing_brand", JObject.FromObject(new
                    {
                        name = string.Empty,
                        website = string.Empty
                    })),
                    new JProperty("publish", string.Empty)
                ).ToJsonContent();
            }
        }


        internal Uri UploadEndpointFor(FileStream asset, string unixTimestamp) =>
            new(new Uri(new($"https://{AwsCredential.Bucket}.s3.amazonaws.com/{AwsCredential.KeyPrefix}"), unixTimestamp + '/'), Path.GetFileName(asset.Name));
    }
}
