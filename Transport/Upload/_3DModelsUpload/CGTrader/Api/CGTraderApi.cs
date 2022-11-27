using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using NLog;
using System.Net.Http.Json;
using Transport.Models;
using Transport.Upload._3DModelsUpload.CGTrader._3DModelComponents;
using Transport.Upload._3DModelsUpload.CGTrader.Network;
using Transport.Upload._3DModelsUpload.CGTrader.Network.Captcha;
using Transport.Upload._3DModelsUpload.CGTrader.Upload;

namespace Transport.Upload._3DModelsUpload.CGTrader.Api;

internal class CGTraderApi : IBaseAddressProvider
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    readonly HttpClient _httpClient;
    readonly CGTraderCaptchaApi _captchaService;

    string IBaseAddressProvider.BaseAddress => CGTraderUri.Https;

    internal CGTraderApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _captchaService = new(httpClient);
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
            const string errorMessage = "Session credentials request failed.";
            _logger.Error(ex, errorMessage); throw new Exception(errorMessage, ex);
        }
    }

    async Task<CGTraderSessionContext> _RequestSessionContextAsyncCore(
        CGTraderNetworkCredential credential,
        CancellationToken cancellationToken)
    {
        string htmlWithSessionCredentials = (await _httpClient.GetStringAsync(
            (this as IBaseAddressProvider).Endpoint("/load-services.js"), cancellationToken)
            ).ReplaceLineEndings(string.Empty);

        string csrfToken = CGTraderCsrfToken._Parse(htmlWithSessionCredentials, CsrfTokenRequest.Initial);
        string siteKey = CGTraderCaptchaSiteKey._Parse(htmlWithSessionCredentials);
        var captcha = await _captchaService._RequestCaptchaAsync(siteKey, cancellationToken);

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
            const string errorMessage = "Login attempt was unsuccessful.";
            _logger.Error(ex, errorMessage);
            throw new HttpRequestException(errorMessage, ex, ex.StatusCode);
        }
    }

    async Task _LoginAsyncCore(CGTraderSessionContext sessionContext, CancellationToken cancellationToken)
    {
        string captchaSolution = await _captchaService._SolveCaptchaAsync(sessionContext.Captcha, cancellationToken);
        using var _ = (await _httpClient.PostAsync(
            (this as IBaseAddressProvider).Endpoint("/users/2fa-or-login.json"),
            sessionContext._LoginMultipartFormDataWith(captchaSolution), cancellationToken))
            ._EnsureSuccessStatusCodeAsync(cancellationToken);
    }

    #endregion

    #region DraftCreation

    /// <inheritdoc cref="__CreateNewModelDraftAsync(CancellationToken)"/>
    internal async Task<string> _CreateNewModelDraftAsync(CGTraderSessionContext sessionContext, CancellationToken cancellationToken)
    {
        try
        {
            string modelDraftId = await __CreateNewModelDraftAsync(sessionContext, cancellationToken);
            _logger.Debug("New model draft with {ID} ID was created.", modelDraftId);
            return modelDraftId;
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
        string htmlWithUploadInitializingCsrfToken = await _RequestUploadInitializingCsrfTokenAsync(cancellationToken);

        _httpClient.DefaultRequestHeaders._AddOrReplaceCsrfToken(
            sessionContext.CsrfToken = CGTraderCsrfToken._Parse(htmlWithUploadInitializingCsrfToken, CsrfTokenRequest.UploadInitializing)
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
            (this as IBaseAddressProvider).Endpoint($"/api/internal/items/current-draft/cg"), new Dictionary<string, string>()
            { { "nocache", CaptchaRequestArguments.rt } }
            );
        return (string)JObject.Parse(
            await _httpClient.GetStringAsync(requestUri, cancellationToken)
            )["data"]!["id"]!;
    }

    #endregion

    #region Upload

    internal async Task _UploadModelAssetsAsyncOf(Composite3DModel composite3DModel, string modelDraftId, CancellationToken cancellationToken)
    {
        foreach (var _3DModel in composite3DModel._3DModels)
            foreach (var modelPart in _3DModel.Files)
                await _UploadModelFileAsync(modelPart, modelDraftId, cancellationToken);

        _UpcastPreviewImagesOf(composite3DModel);
        foreach (var preview in composite3DModel.PreviewImages.Select(preview => (preview as CGTrader3DModelPreviewImage)!))
        {
            string uploadedFileId = await _UploadModelPreviewImageAsync(preview, modelDraftId, cancellationToken);
            (composite3DModel.Metadata as CGTrader3DModelMetadata)!.UploadedPreviewImagesIDs.Add(uploadedFileId);
        }
    }

    #region ModelFileUpload

    async Task _UploadModelFileAsync(string modelFilePath, string modelDraftId, CancellationToken cancellationToken)
    {
        try
        {
            await __UploadModelFileAsync(modelFilePath, modelDraftId, cancellationToken);
            _logger.Debug("3D model file at {Path} was uploaded to {ModelDraftID} model draft.", modelFilePath, modelDraftId);
        }
        catch (HttpRequestException ex)
        {
            string errorMessage = $"3D model file at {modelFilePath} couldn't be uploaded to {modelDraftId} model draft.";
            throw new HttpRequestException(errorMessage, ex, ex.StatusCode);
        }
    }

    async Task __UploadModelFileAsync(string modelFilePath, string modelDraftId, CancellationToken cancellationToken)
    {
        var modelFileUploadSessionData = await _ReserveServerSpaceAsyncFor(modelFilePath, modelDraftId, cancellationToken);
        await modelFileUploadSessionData._UseToUploadWith(_httpClient, HttpMethod.Post, cancellationToken);
    }

    /// <returns><see cref="CGTrader3DModelFileUploadSessionData"/> for the file at <paramref name="modelFilePath"/>.</returns>
    async Task<CGTrader3DModelFileUploadSessionData> _ReserveServerSpaceAsyncFor(
        string modelFilePath,
        string modelDraftId,
        CancellationToken cancellationToken)
    {
        using var modelFileStream = File.OpenRead(modelFilePath);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            (this as IBaseAddressProvider).Endpoint($"/profile/items/{modelDraftId}/uploads")
            )
        { Content = modelFileStream._ToModelFileMultipartFormDataOptions() };

        using var response = (await _httpClient.SendAsync(request._WithHostHeader(), cancellationToken))
            .EnsureSuccessStatusCode();

        return await CGTrader3DModelAssetUploadSessionData
            ._ForModelFileAsyncFrom(response, modelFilePath, cancellationToken);
    }

    #endregion

    #region ModelPreviewUpload

    static void _UpcastPreviewImagesOf(Composite3DModel composite3DModel) =>
        composite3DModel.PreviewImages = composite3DModel.PreviewImages
        .Select(previewImage => new CGTrader3DModelPreviewImage(previewImage.FilePath));

    async Task<string> _UploadModelPreviewImageAsync(CGTrader3DModelPreviewImage modelPreview, string modelDraftId, CancellationToken cancellationToken)
    {
        try
        {
            string uploadedFileId = await __UploadModelPreviewImageAsync(modelPreview, modelDraftId, cancellationToken);
            _logger.Debug("3D model preview image at {Path} was uploaded to {ModelDraft} model draft with {UploadedFileID} ID.",
                modelPreview.FilePath, uploadedFileId
                );
            return uploadedFileId;
        }
        catch (Exception ex)
        {
            string errorMessage = $"3D model preview image at {modelPreview.FilePath} couldn't be uploaded to {modelDraftId} model draft.";
            _logger.Error(ex, errorMessage); throw new Exception(errorMessage, ex);
        }
    }

    async Task<string> __UploadModelPreviewImageAsync(CGTrader3DModelPreviewImage modelPreview, string modelDraftId, CancellationToken cancellationToken)
    {
        var modelPreviewImageUploadSessionData = await _ReserveServerSpaceAsyncFor(modelPreview, cancellationToken);
        await _SendModelPreviewImageUploadOptionsAsync(modelPreviewImageUploadSessionData, cancellationToken);
        await _UploadModelPreviewImageAsyncCore(modelPreviewImageUploadSessionData, cancellationToken);
        return await _RequestUploadedFileIDAsync(modelPreviewImageUploadSessionData, modelDraftId, cancellationToken);
    }

    /// <returns><see cref="CGTrader3DModelPreviewImageUploadSessionData"/> for the <paramref name="modelPreviewImage"/>.</returns>
    async Task<CGTrader3DModelPreviewImageUploadSessionData> _ReserveServerSpaceAsyncFor(
        CGTrader3DModelPreviewImage modelPreviewImage,
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
                        filename = modelPreviewImage.Name,
                        content_type = modelPreviewImage.MimeType.MediaType,
                        byte_size = fileStream.Length
                    }
                })
            };
        using var response = (await _httpClient.SendAsync(request, cancellationToken))
            .EnsureSuccessStatusCode();

        return await CGTrader3DModelAssetUploadSessionData._ForModelPreviewImageAsyncFrom(
            response,
            modelPreviewImage.FilePath,
            cancellationToken);
    }

    async Task _SendModelPreviewImageUploadOptionsAsync(
        CGTrader3DModelPreviewImageUploadSessionData modelPreviewImageUploadSessionData,
        CancellationToken cancellationToken) =>
            await modelPreviewImageUploadSessionData._UseToUploadWith(_httpClient, HttpMethod.Options, cancellationToken);

    async Task _UploadModelPreviewImageAsyncCore(
        CGTrader3DModelPreviewImageUploadSessionData modelPreviewImageUploadSessionData,
        CancellationToken cancellationToken) =>
            await modelPreviewImageUploadSessionData._UseToUploadWith(_httpClient, HttpMethod.Put, cancellationToken);

    async Task<string> _RequestUploadedFileIDAsync(
        CGTrader3DModelPreviewImageUploadSessionData modelPreviewImageUploadSessionData,
        string modelDraftId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put,
            (this as IBaseAddressProvider).Endpoint($"/api/internal/direct-uploads/item-images/{modelPreviewImageUploadSessionData._SignedFileID}")
            ) { Content = JsonContent.Create(new { item_id = modelDraftId }) };

        using var response = (await _httpClient.SendAsync(request._WithHostHeader(), cancellationToken)).EnsureSuccessStatusCode();

        return (string)JObject.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken)
            )["data"]!["attributes"]!["id"]!;
    }

    #endregion

    #region ModelMetadataUpload

    internal async Task _UploadModelMetadataAsync(CGTrader3DModelMetadata metadata, string modelDraftId, CancellationToken cancellationToken)
    {
        try
        {
            await _UploadModelMetadataAsyncCore(metadata, modelDraftId, cancellationToken);
            _logger.Debug("Metadata was uploaded to {ModelDraftID} model draft.", modelDraftId);
        }
        catch (Exception ex)
        {
            string errorMessage = $"Metadata couldn't be uploaded to {modelDraftId} model draft.";
            _logger.Error(ex, errorMessage); throw new Exception(errorMessage, ex);
        }
    }

    async Task _UploadModelMetadataAsyncCore(CGTrader3DModelMetadata metadata, string modelDraftId, CancellationToken cancellationToken) =>
        await (await _httpClient.PatchAsync(
            (this as IBaseAddressProvider).Endpoint($"/profile/items/{modelDraftId}"),
            metadata._AsCGJsonContent,
            cancellationToken)
        )._EnsureSuccessStatusCodeAsync(cancellationToken); // mb make CGTrader3DModelMetadata abstract and inherit cg and printable classes from it.

    #endregion

    #region Publishing

    internal async Task _PublishModelAsync(Composite3DModel composite3DModel, string modelDraftId, CancellationToken cancellationToken)
    {
        try
        {
            await _PublishModelAsyncCore(composite3DModel, modelDraftId, cancellationToken);
            _logger.Debug("Model with {ModelDraftID} ID was published.", modelDraftId);
        }
        catch (Exception ex)
        {
            string errorMessage = $"Model with {modelDraftId} ID couldn't be published.";
            _logger.Error(ex, errorMessage); throw new Exception(errorMessage, ex);
        }
    }
        

    async Task _PublishModelAsyncCore(Composite3DModel composite3DModel, string modelDraftId, CancellationToken cancellationToken) =>
        await(await _httpClient.PostAsJsonAsync(
            (this as IBaseAddressProvider).Endpoint($"/profile/items/{modelDraftId}/publish"),
            new { item = new { tags = (composite3DModel.Metadata as CGTrader3DModelMetadata)!.Tags } },
            cancellationToken)
        )._EnsureSuccessStatusCodeAsync(cancellationToken);

    #endregion

    #endregion
}

static class Extensions
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
