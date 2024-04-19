using System.Net.Http.Json;
using _3DProductsPublish._3DProductDS;
using static MarkTM.RFProduct.RFProduct._3D;
using System.Security.Cryptography;
using _3DProductsPublish.CGTrader.Captcha;
using _3DProductsPublish.CGTrader.Upload;

namespace _3DProductsPublish.CGTrader;

public partial class CGTrader
{
    public class DraftSession(_3DProduct _3DProduct, CGTrader client, CancellationToken cancellationToken)
    {
        _3DProduct _3DProduct { get; } = _3DProduct;
        CGTrader Client { get; } = client;
        CancellationToken CancellationToken { get; } = cancellationToken;
        readonly HashSet<string> removed_image_ids = [];

        internal static async Task<DraftSession> InitializeAsync(_3DProduct _3DProduct, CGTrader client, CancellationToken cancellationToken)
        {
            try
            {
                if (_3DProduct.Tracker.Data.DraftID is 0)
                    _3DProduct.Tracker.Data.DraftID = await NewAsync();
                else await EditAsync();
                _3DProduct.Tracker.Data.Status = Status.draft;
                _3DProduct.Tracker.Write();
                _logger.Debug("New draft with {ID} ID was created.", _3DProduct.Tracker.Data.DraftID);
                return new(_3DProduct, client, cancellationToken);
            }
            catch (Exception ex)
            {
                const string errorMessage = "New draft couldn't be created.";
                _logger.Error(ex, errorMessage); throw new Exception(errorMessage, ex);
            }


            async Task<long> NewAsync()
            {
                client.DefaultRequestHeaders.Remove(AuthenticityToken.Header);
                client.DefaultRequestHeaders.Add(AuthenticityToken.Header, AuthenticityToken.ParseFromMetaTag(await client.GetStringAsync("profile/upload/model", cancellationToken)));
                var draftDocument = await client.GetStringAsync($"api/internal/items/current-draft?nocache={CGTraderCaptchaRequestArguments.rt}", cancellationToken);
                return JObject.Parse(draftDocument)["data"]?["id"]?.Value<long>() ??
                    throw new HttpRequestException($"{nameof(draftDocument)} doesn't contain draft ID.");
            }

            async Task EditAsync()
            {
                client.DefaultRequestHeaders.Remove(AuthenticityToken.Header);
                client.DefaultRequestHeaders.Add(AuthenticityToken.Header, AuthenticityToken.ParseFromMetaTag(await client.GetStringAsync($"profile/models/{_3DProduct.Tracker.Data.DraftID}/edit", cancellationToken)));
                await client.GetStringAsync($"api/internal/items/{_3DProduct.Tracker.Data.DraftID}?nocache={CGTraderCaptchaRequestArguments.rt}", cancellationToken);
            }
        }

        internal async Task UploadAssetsAsync()
        {
            await DesynchronizeOutdatedAssetsAsync<_3DModel>();
            foreach (var _3DModel in _3DProduct._3DModels.Where(_ => _3DProduct.Tracker.Model(_).ID == default))
                await UploadModelAsyncCore(_3DModel);

            await DesynchronizeOutdatedAssetsAsync<_3DProductThumbnail>();
            foreach (var modelPreviewImage in _3DProduct.Thumbnails.Where(_ => _3DProduct.Tracker.Preview(_).ID == default))
                await UploadPreviewAsync(modelPreviewImage);
        }

        async Task UploadModelAsyncCore(_3DModel _3DModel)
        {
            try
            {
                await UploadModelAsyncCore();
                _logger.Debug("3D model file at {Path} was uploaded to {ModelDraftID} draft.", _3DModel.Path, _3DProduct.Tracker.Data.DraftID);
            }
            catch (HttpRequestException ex)
            {
                string errorMessage = $"3D model file at {_3DModel.Path} couldn't be uploaded to {_3DProduct.Tracker.Data.DraftID} draft.";
                throw new HttpRequestException(errorMessage, ex, ex.StatusCode);
            }


            async Task UploadModelAsyncCore()
            {
                var modelFileUploadSessionData = await ReserveServerSpaceAsync();
                // Previews require extra request to be sent with this FileID as parameter to obtain ID for tracking.
                var id = modelFileUploadSessionData._FileID;
                _3DProduct.Tracker.Model(_3DModel).Update(modelFileUploadSessionData._FileID);
                _3DProduct.Tracker.Write();
                await modelFileUploadSessionData.UseToUploadWith(Client, HttpMethod.Post, CancellationToken);
                await UploadModelMetadataAsync();


                async Task<CGTrader3DModelFileUploadSessionData> ReserveServerSpaceAsync()
                {
                    using var modelFileStream = File.OpenRead(_3DModel.Path);
                    var filename = new StringContent(Path.GetFileName(modelFileStream.Name)); filename.Headers.ContentType = null;
                    var type = new StringContent("file"); type.Headers.ContentType = null;

                    using var request = new HttpRequestMessage(HttpMethod.Post, $"profile/items/{_3DProduct.Tracker.Data.DraftID}/uploads")
                    { Content = new MultipartFormDataContent() { { filename, "filename" }, { type, "type" } } }
                    .WithHostHeader();
                    using var response = (await Client.SendAsync(request, CancellationToken)).EnsureSuccessStatusCode();

                    return await CGTrader3DModelAssetUploadSessionData._ForModelFileAsyncFrom(response, _3DModel.Path, CancellationToken);
                }

                async Task UploadModelMetadataAsync()
                {
                    using var modelFileStream = File.OpenRead(_3DModel.Path);
                    var name = Path.GetFileName(modelFileStream.Name);
                    var ifiId = await RequestItemFilesInfoIDAsync();
                    await UploadModelMetadataAsyncCore();


                    async Task<long> RequestItemFilesInfoIDAsync()
                    {
                        var request = new HttpRequestMessage(
                            HttpMethod.Put,
                            $"api/internal/items/{_3DProduct.Tracker.Data.DraftID}/item_files/{modelFileUploadSessionData._FileID}")
                        {
                            Content = JsonContent.Create(new
                            {
                                key = $"uploads/files/{_3DProduct.Tracker.Data.DraftID}/{name}",
                                filename = name,
                                filesize = modelFileStream.Length,
                            })
                        };
                        return JObject.Parse(await (await Client.SendAsync(request, CancellationToken)).EnsureSuccessStatusCode().Content.ReadAsStringAsync(CancellationToken))
                            ["data"]!["relationships"]!["itemFileInfos"]!["data"]!.First!["id"]!.Value<long>();
                    }

                    async Task UploadModelMetadataAsyncCore()
                    {
                        var metadata = _3DProduct.Tracker.Model(_3DModel)?._ ?? throw new InvalidOperationException();
                        var request = new HttpRequestMessage(HttpMethod.Patch, $"api/internal/items/{_3DProduct.Tracker.Data.DraftID}/item_files/{modelFileUploadSessionData._FileID}/item_file_infos/{ifiId}")
                        {
                            Content = JsonContent.Create(new
                            {
                                item_file_info = new
                                {
                                    native_format = metadata.IsNative,
                                    file_type_id = metadata.FileFormat,
                                    version = metadata.FormatVersion,
                                    renderer_id = 150,  // Default
                                    renderer_version = metadata.RendererVersion
                                }
                            })
                        };
                        (await Client.SendAsync(request, CancellationToken)).EnsureSuccessStatusCode();
                    }
                }
            }
        }

        async Task UploadPreviewAsync(_3DProductThumbnail preview)
        {
            try
            {
                await UploadPreviewAsyncCore();
                _logger.Debug("3D model thumbnail at {Path} was uploaded to {DraftID} draft.", preview.Path, _3DProduct.Tracker.Data.DraftID);
            }
            catch (Exception ex)
            {
                string errorMessage = $"3D model thumbnail at {preview.Path} couldn't be uploaded to {_3DProduct.Tracker.Data.DraftID} draft.";
                _logger.Error(ex, errorMessage); throw new Exception(errorMessage, ex);
            }


            async Task UploadPreviewAsyncCore()
            {
                var modelPreviewImageUploadSessionData = await ReserveServerSpaceAsync();
                await SendPreviewUploadOptionsAsync();
                await UploadPreviewAsyncCore();
                var id = await RequestTrackerID();

                _3DProduct.Tracker.Preview(preview).Update(id);
                _3DProduct.Tracker.Write();


                async Task<CGTrader3DModelPreviewImageUploadSessionData> ReserveServerSpaceAsync()
                {
                    using var fileStream = File.OpenRead(preview.Path);
                    using var hasher = MD5.Create();
                    using var request = new HttpRequestMessage(
                        HttpMethod.Post,
                        "api/internal/direct-uploads/item-images")
                    {
                        Content = JsonContent.Create(new
                        {
                            blob = new
                            {
                                checksum = Convert.ToBase64String(await hasher.ComputeHashAsync(fileStream, CancellationToken)),
                                filename = preview.Name(),
                                content_type = preview.MimeType().MediaType,
                                byte_size = fileStream.Length
                            }
                        })
                    };
                    using var response = (await Client.SendAsync(request, CancellationToken)).EnsureSuccessStatusCode();

                    return await CGTrader3DModelAssetUploadSessionData._ForModelThumbnailAsyncFrom(response, preview.Path, CancellationToken);
                }

                async Task SendPreviewUploadOptionsAsync()
                    => await modelPreviewImageUploadSessionData.UseToUploadWith(Client, HttpMethod.Options, CancellationToken);

                async Task UploadPreviewAsyncCore()
                    => await modelPreviewImageUploadSessionData.UseToUploadWith(Client, HttpMethod.Put, CancellationToken);

                async Task<long> RequestTrackerID()
                {
                    using var request = new HttpRequestMessage(
                        HttpMethod.Put,
                        $"api/internal/direct-uploads/item-images/{modelPreviewImageUploadSessionData._SignedFileID}")
                    { Content = JsonContent.Create(new { item_id = _3DProduct.Tracker.Data.DraftID }) }
                    .WithHostHeader();
                    using var response = (await Client.SendAsync(request, CancellationToken)).EnsureSuccessStatusCode();

                    return JObject.Parse(await response.Content.ReadAsStringAsync(CancellationToken))
                        ["data"]!["id"]!.Value<long>()!;
                }
            }
        }

        internal async Task UploadMetadataAsync()
        {
            try
            {
                await
                    (await Client.PatchAsync($"profile/items/{_3DProduct.Tracker.Data.DraftID}",
                    _3DProduct.Metadata.ToProductForm(_3DProduct.Tracker.Data.Previews.Select(_ => _.ID.ToString()), removed_image_ids),
                    CancellationToken))
                .EnsureSuccessStatusCodeAsync(CancellationToken);
                _logger.Debug("Metadata was uploaded to {ModelDraftID} model draft.", _3DProduct.Tracker.Data.DraftID);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Metadata couldn't be uploaded to {_3DProduct.Tracker.Data.DraftID} model draft.";
                _logger.Error(ex, errorMessage); throw new Exception(errorMessage, ex);
            }
        }

        internal async Task PublishAsync()
        {
            try
            {
                await
                    (await Client.PostAsJsonAsync($"profile/items/{_3DProduct.Tracker.Data.DraftID}/publish",
                    new { item = new { tags = _3DProduct.Metadata.Tags } },
                    CancellationToken))
                .EnsureSuccessStatusCodeAsync(CancellationToken);
                removed_image_ids.Clear();
                _3DProduct.Tracker.Data.Status = Status.online;
                _3DProduct.Tracker.Write();
                _logger.Debug("_3DProduct with {DraftID} ID was published.", _3DProduct.Tracker.Data.DraftID);
            }
            catch (Exception ex)
            {
                string errorMessage = $"{nameof(_3DProduct)} with {_3DProduct.Tracker.Data.DraftID} ID couldn't be published.";
                _logger.Error(ex, errorMessage); throw new Exception(errorMessage, ex);
            }
        }

        internal async Task DesynchronizeOutdatedAssetsAsync<TAsset>()
            where TAsset : class, I3DProductAsset
        {
            _logger.Trace($"Desynchronizing outdated {typeof(TAsset).Name} assets.");
            if (typeof(TAsset) == typeof(_3DModel))
                await DesynchronizeOutdatedModelsAsync();
            else if (typeof(TAsset) == typeof(_3DProductThumbnail))
                await DesynchronizeOutdatedPreviewsAsync();
            _logger.Trace($"Desynchronization of outdated {typeof(TAsset).Name} assets completed.");


            async Task DesynchronizeOutdatedModelsAsync()
            {
                foreach (var _ in _3DProduct._3DModels.Select(_ => new { Asset = _, Tracker = _3DProduct.Tracker.Model(_) })
                    .Where(_ => File.GetLastWriteTimeUtc(_.Asset.Archived) > _.Tracker.LastWriteTime))
                {
                    await DeleteAssetAsync(_.Tracker);
                    _.Tracker.Update(id: default); _3DProduct.Tracker.Write();
                }
            }

            async Task DesynchronizeOutdatedPreviewsAsync()
            {
                foreach (var _ in _3DProduct.Thumbnails.Select(_ => new { Asset = _, Tracker = _3DProduct.Tracker.Preview(_) })
                    .Where(_ => File.GetLastWriteTimeUtc(_.Asset.Path) > _.Tracker.LastWriteTime))
                {
                    await DeleteAssetAsync(_.Tracker);
                    _.Tracker.Update(id: default); _3DProduct.Tracker.Write();
                }
            }
        }

        async Task DeleteAssetAsync<T>(_3DProduct.Tracker_.Target<T> asset)
            where T : class
        {
            try
            {
                string resource = typeof(T) switch
                {
                    _ when typeof(T) == typeof(_3DModelMetadata) => $"profile/items/{_3DProduct.Tracker.Data.DraftID}/uploads/{asset.ID}",
                    _ when typeof(T) == typeof(_3DProduct.Tracker_.CGTraderAssetMetadata) => $"api/internal/items/{_3DProduct.Tracker.Data.DraftID}/images/{asset.ID}",
                    _ => throw new NotImplementedException()
                };
                await Client.DeleteAsync(resource, CancellationToken);
                removed_image_ids.Add(asset.ID.ToString());
            }
            catch (Exception ex)
            { throw new HttpRequestException($"Asset deletion from remote failed.", ex); }
        }
    }
}
