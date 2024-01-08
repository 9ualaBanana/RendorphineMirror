using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.CGTrader._3DModelComponents;
using _3DProductsPublish.CGTrader.Network;
using _3DProductsPublish.CGTrader.Network.Captcha;
using _3DProductsPublish.CGTrader.Upload;
using _3DProductsPublish.CGTrader.Upload.SessionData;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;
using static _3DProductsPublish._3DProductDS._3DProduct;

namespace _3DProductsPublish.CGTrader.Api;

internal class CGTraderApi : IBaseAddressProvider
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    readonly HttpClient _httpClient;
    readonly CGTraderCaptchaApi _captchaService;

    string IBaseAddressProvider.BaseAddress => CGTraderUri.Https;

    internal CGTraderApi(HttpClient httpClient, CGTraderCaptchaApi captchaService)
    {
        _httpClient = httpClient;
        _captchaService = captchaService;
    }

    #region Login

    #region SessionCredentials

    internal async Task<CGTraderSessionContext> _RequestSessionContextAsync(
        CGTraderNetworkCredential credential,
        CancellationToken cancellationToken)
    {
        try
        {
            var sessionContext = await _RequestSessionContextAsyncCore(credential, cancellationToken);
            _logger.Debug("Session context was received.");
            return sessionContext;
        }
        catch (Exception ex)
        {
            const string errorMessage = "Session context request failed.";
            _logger.Error(ex, errorMessage); throw new Exception(errorMessage, ex);
        }
    }

    async Task<CGTraderSessionContext> _RequestSessionContextAsyncCore(
        CGTraderNetworkCredential credential,
        CancellationToken cancellationToken)
    {
        string documentWithSessionCredentials = (await _httpClient.GetStringAsync(
            (this as IBaseAddressProvider).Endpoint("/load-services.js"), cancellationToken)
            ).ReplaceLineEndings(string.Empty);

        string csrfToken = CsrfToken._ParseFromJS(documentWithSessionCredentials);
        string siteKey = CGTraderCaptchaSiteKey._Parse(documentWithSessionCredentials);
        var captcha = await _captchaService._RequestCaptchaAsync(siteKey, cancellationToken);

        _httpClient.DefaultRequestHeaders._AddOrReplaceCsrfToken(csrfToken);

        return new CGTraderSessionContext(credential, csrfToken, captcha);
    }

    #endregion

    internal async Task _LoginAsync(CGTraderSessionContext sessionContext, CancellationToken cancellationToken)
    {
        try
        {
            await _LoginAsyncCore(sessionContext, cancellationToken);
            _logger.Debug("{User} is successfully logged in.", sessionContext.Credential.UserName);
        }
        catch (HttpRequestException ex)
        {
            string errorMessage = $"Login attempt for {sessionContext.Credential.UserName} was unsuccessful.";
            _logger.Error(ex, errorMessage);
            throw new HttpRequestException(errorMessage, ex, ex.StatusCode);
        }
    }

    async Task _LoginAsyncCore(CGTraderSessionContext sessionContext, CancellationToken cancellationToken)
    {
        string verifiedToken = await _captchaService._SolveCaptchaAsync(sessionContext.Captcha, cancellationToken);
        using var _ = (await _httpClient.PostAsync(
            (this as IBaseAddressProvider).Endpoint("/users/2fa-or-login.json"),
            sessionContext._LoginMultipartFormDataWith(verifiedToken), cancellationToken))
            ._EnsureSuccessStatusCodeAsync(cancellationToken);
    }

    #endregion

    #region DraftCreation

    /// <inheritdoc cref="__CreateNewModelDraftAsync(CancellationToken)"/>
    internal async Task<_3DProductDraft<CGTrader3DProductMetadata>> _CreateNewModelDraftAsyncFor(
        _3DProduct<CGTrader3DProductMetadata> composite3DModel,
        CGTraderSessionContext sessionContext,
        CancellationToken cancellationToken)
    {
        try
        {
            string modelDraftId = await __CreateNewModelDraftAsync(sessionContext, cancellationToken);
            _logger.Debug("New model draft with {ID} ID was created.", modelDraftId);
            return new(composite3DModel, modelDraftId);
        }
        catch (Exception ex)
        {
            const string errorMessage = "New model draft couldn't be created.";
            _logger.Error(ex, errorMessage); throw new Exception(errorMessage, ex);
        }
    }

    /// <returns>ID of the newly created model draft.</returns>
    async Task<string> __CreateNewModelDraftAsync(CGTraderSessionContext sessionContext, CancellationToken cancellationToken)
    {
        string documentWithUploadInitializingCsrfToken = await _RequestUploadInitializingCsrfTokenAsync(cancellationToken);

        _httpClient.DefaultRequestHeaders._AddOrReplaceCsrfToken(
            sessionContext.CsrfToken = CsrfToken._ParseFromMetaTag(documentWithUploadInitializingCsrfToken)
            );
        string modelDraftId = await _CreateNewModelDraftAsyncCore(cancellationToken);

        return modelDraftId;
    }

    async Task<string> _RequestUploadInitializingCsrfTokenAsync(CancellationToken cancellationToken) =>
        await _httpClient.GetStringAsync(
            (this as IBaseAddressProvider).Endpoint("/profile/upload/model"),
            cancellationToken
            );

    async Task<string> _CreateNewModelDraftAsyncCore(CancellationToken cancellationToken)
    {
        string requestUri = QueryHelpers.AddQueryString(
            (this as IBaseAddressProvider).Endpoint($"/api/internal/items/current-draft"), new Dictionary<string, string>()
            { { "nocache", CaptchaRequestArguments.rt } }
);
        return (string)JObject.Parse(
            await _httpClient.GetStringAsync(requestUri, cancellationToken)
            )["data"]!["id"]!;
    }

    #endregion

    #region Upload

    internal async Task _UploadAssetsAsync(_3DProductDraft<CGTrader3DProductMetadata> _3DProductDraft, CancellationToken cancellationToken)
    {
        foreach (var _3DModel in _3DProductDraft._Product._3DModels)
            foreach (var modelFilePath in _3DModel.EnumerateEntries(AssetContainer.EntryType.NonContainers))
                await _UploadModelFileAsync(modelFilePath, _3DProductDraft, cancellationToken);

        foreach (var modelPreviewImage in _3DProductDraft.UpcastThumbnailsTo<CGTrader3DModelThumbnail>())
        {
            string uploadedFileId = await _UploadModelThumbnailAsync(modelPreviewImage, _3DProductDraft, cancellationToken);
            (_3DProductDraft._Product.Metadata as CGTrader3DProductMetadata)!.UploadedPreviewImagesIDs.Add(uploadedFileId);
        }
    }

    #region ModelFileUpload

    async Task _UploadModelFileAsync(string modelFilePath, _3DProductDraft<CGTrader3DProductMetadata> modelDraft, CancellationToken cancellationToken)
    {
        try
        {
            await __UploadModelFileAsync(modelFilePath, modelDraft, cancellationToken);
            _logger.Debug("3D model file at {Path} was uploaded to {ModelDraftID} model draft.", modelFilePath, modelDraft._ID);
        }
        catch (HttpRequestException ex)
        {
            string errorMessage = $"3D model file at {modelFilePath} couldn't be uploaded to {modelDraft._ID} model draft.";
            throw new HttpRequestException(errorMessage, ex, ex.StatusCode);
        }
    }

    async Task __UploadModelFileAsync(string modelFilePath, _3DProductDraft<CGTrader3DProductMetadata> modelDraft, CancellationToken cancellationToken)
    {
        var modelFileUploadSessionData = await _ReserveServerSpaceAsyncFor(modelFilePath, modelDraft, cancellationToken);
        await modelFileUploadSessionData._UseToUploadWith(_httpClient, HttpMethod.Post, cancellationToken);
    }

    /// <returns><see cref="CGTrader3DModelFileUploadSessionData"/> for the file at <paramref name="modelFilePath"/>.</returns>
    async Task<CGTrader3DModelFileUploadSessionData> _ReserveServerSpaceAsyncFor(
        string modelFilePath,
        _3DProductDraft<CGTrader3DProductMetadata> modelDraft,
        CancellationToken cancellationToken)
    {
        using var modelFileStream = File.OpenRead(modelFilePath);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            (this as IBaseAddressProvider).Endpoint($"/profile/items/{modelDraft._ID}/uploads")
            )
        { Content = modelFileStream._ToModelFileMultipartFormDataOptions() };

        using var response = (await _httpClient.SendAsync(request._WithHostHeader(), cancellationToken))
            .EnsureSuccessStatusCode();

        return await CGTrader3DModelAssetUploadSessionData
            ._ForModelFileAsyncFrom(response, modelFilePath, cancellationToken);
    }

    #endregion

    #region ModelThumbnails

    async Task<string> _UploadModelThumbnailAsync(
        CGTrader3DModelThumbnail modelThumbnail,
        _3DProductDraft<CGTrader3DProductMetadata> modelDraft,
        CancellationToken cancellationToken)
    {
        try
        {
            string uploadedFileId = await __UploadModelThumbnailAsync(modelThumbnail, modelDraft, cancellationToken);
            _logger.Debug("3D model thumbnail at {Path} was uploaded to {ModelDraftID} model draft with {UploadedFileID} ID.",
                modelThumbnail.FilePath, modelDraft._ID, uploadedFileId
                );
            return uploadedFileId;
        }
        catch (Exception ex)
        {
            string errorMessage = $"3D model thumbnail at {modelThumbnail.FilePath} couldn't be uploaded to {modelDraft._ID} model draft.";
            _logger.Error(ex, errorMessage); throw new Exception(errorMessage, ex);
        }
    }
    async Task<string> __UploadModelThumbnailAsync(
        CGTrader3DModelThumbnail modelThumbnail,
        _3DProductDraft<CGTrader3DProductMetadata> modelDraft,
        CancellationToken cancellationToken)
    {
        var modelThumbnailUploadSessionData = await _ReserveServerSpaceAsyncFor(modelThumbnail, cancellationToken);
        await _SendModelThumbnailUploadOptionsAsync(modelThumbnailUploadSessionData, cancellationToken);
        await _UploadModelThumbnailAsyncCore(modelThumbnailUploadSessionData, cancellationToken);
        return await _RequestUploadedFileIDAsync(modelThumbnailUploadSessionData, modelDraft, cancellationToken);
    }

    /// <returns><see cref="CGTrader3DModelPreviewImageUploadSessionData"/> for the <paramref name="modelPreviewImage"/>.</returns>
    async Task<CGTrader3DModelPreviewImageUploadSessionData> _ReserveServerSpaceAsyncFor(
        CGTrader3DModelThumbnail modelPreviewImage,
        CancellationToken cancellationToken)
    {
        using var fileStream = modelPreviewImage.AsFileStream;
        using var request = new HttpRequestMessage(HttpMethod.Post,
            (this as IBaseAddressProvider).Endpoint("/api/internal/direct-uploads/item-images"))
        {
            Content = JsonContent.Create(new
            {
                blob = new
                {
                    checksum = await modelPreviewImage.ChecksumAsync(cancellationToken),
                    filename = modelPreviewImage.FileName,
                    content_type = modelPreviewImage.MimeType.MediaType,
                    byte_size = fileStream.Length
                }
            })
        };
        using var response = (await _httpClient.SendAsync(request, cancellationToken))
            .EnsureSuccessStatusCode();

        return await CGTrader3DModelAssetUploadSessionData._ForModelThumbnailAsyncFrom(
            response,
            modelPreviewImage.FilePath,
            cancellationToken);
    }

    async Task _SendModelThumbnailUploadOptionsAsync(
        CGTrader3DModelPreviewImageUploadSessionData modelPreviewImageUploadSessionData,
        CancellationToken cancellationToken) =>
            await modelPreviewImageUploadSessionData._UseToUploadWith(_httpClient, HttpMethod.Options, cancellationToken);

    async Task _UploadModelThumbnailAsyncCore(
        CGTrader3DModelPreviewImageUploadSessionData modelPreviewImageUploadSessionData,
        CancellationToken cancellationToken) =>
            await modelPreviewImageUploadSessionData._UseToUploadWith(_httpClient, HttpMethod.Put, cancellationToken);

    async Task<string> _RequestUploadedFileIDAsync(
        CGTrader3DModelPreviewImageUploadSessionData modelPreviewImageUploadSessionData,
        _3DProductDraft<CGTrader3DProductMetadata> modelDraft,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put,
            (this as IBaseAddressProvider).Endpoint($"/api/internal/direct-uploads/item-images/{modelPreviewImageUploadSessionData._SignedFileID}")
            )
        { Content = JsonContent.Create(new { item_id = modelDraft._ID }) };

        using var response = (await _httpClient.SendAsync(request._WithHostHeader(), cancellationToken)).EnsureSuccessStatusCode();

        return (string)JObject.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken)
            )["data"]!["attributes"]!["id"]!;
    }

    #endregion

    #region ModelMetadataUpload

    internal async Task _UploadMetadataAsync(_3DProductDraft<CGTrader3DProductMetadata> modelDraft, CancellationToken cancellationToken)
    {
        try
        {
            await _UploadModelMetadataAsyncCore(modelDraft, cancellationToken);
            _logger.Debug("Metadata was uploaded to {ModelDraftID} model draft.", modelDraft._ID);
        }
        catch (Exception ex)
        {
            string errorMessage = $"Metadata couldn't be uploaded to {modelDraft._ID} model draft.";
            _logger.Error(ex, errorMessage); throw new Exception(errorMessage, ex);
        }
    }

    async Task _UploadModelMetadataAsyncCore(_3DProductDraft<CGTrader3DProductMetadata> _3DProductDraft, CancellationToken cancellationToken) =>
        await (await _httpClient.PatchAsync(
            (this as IBaseAddressProvider).Endpoint($"/profile/items/{_3DProductDraft._ID}"),
            (_3DProductDraft._Product.Metadata as CGTrader3DProductMetadata)!._AsCGJsonContent,
            cancellationToken)
        )._EnsureSuccessStatusCodeAsync(cancellationToken); // mb make CGTrader3DModelMetadata abstract and inherit cg and printable classes from it.

    #endregion

    #region Publishing

    internal async Task _PublishAsync(_3DProductDraft<CGTrader3DProductMetadata> modelDraft, CancellationToken cancellationToken)
    {
        try
        {
            await _PublishProductAsyncCore(modelDraft, cancellationToken);
            _logger.Debug("Model with {ModelDraftID} ID was published.", modelDraft._ID);
        }
        catch (Exception ex)
        {
            string errorMessage = $"Model with {modelDraft._ID} ID couldn't be published.";
            _logger.Error(ex, errorMessage); throw new Exception(errorMessage, ex);
        }
    }

    async Task _PublishProductAsyncCore(_3DProductDraft<CGTrader3DProductMetadata> _3DProductDraft, CancellationToken cancellationToken) =>
        await (await _httpClient.PostAsJsonAsync(
            (this as IBaseAddressProvider).Endpoint($"/profile/items/{_3DProductDraft._ID}/publish"),
            new { item = new { tags = (_3DProductDraft._Product.Metadata as CGTrader3DProductMetadata)!.Tags } },
            cancellationToken)
        )._EnsureSuccessStatusCodeAsync(cancellationToken);

    #endregion

    #endregion
}

static class CGTraderApiExtensions
{
    internal static async Task _EnsureSuccessStatusCodeAsync(this HttpResponseMessage response, CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();

        string responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseJson = JObject.Parse(responseText);

        if ((bool)responseJson["success"]! != true)
            throw new HttpRequestException("The value of `success` field in the response is not `true`.");
    }

    internal static MultipartFormDataContent _ToModelFileMultipartFormDataOptions(this FileStream fileStream)
    {
        var filename = new StringContent(Path.GetFileName(fileStream.Name)); filename.Headers.ContentType = null;
        var type = new StringContent("file"); type.Headers.ContentType = null;
        return new MultipartFormDataContent() { { filename, "filename" }, { type, "type" } };
    }

    internal static HttpRequestMessage _WithHostHeader(this HttpRequestMessage request)
    {
        request.Headers.Host = CGTraderUri.www;
        return request;
    }
}
