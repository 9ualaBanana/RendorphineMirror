using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using NLog;
using System.Net.Http.Json;
using Transport.Models;
using Transport.Upload._3DModelsUpload.CGTrader._3DModelComponents;
using Transport.Upload._3DModelsUpload.CGTrader.Captcha;
using Transport.Upload._3DModelsUpload.CGTrader.Upload;

namespace Transport.Upload._3DModelsUpload.CGTrader.Api;

internal class CGTraderApi : IBaseAddressProvider
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    const string _CsrfTokenMeta = "<meta name=\"csrf-token\" content=\"";

    readonly HttpClient _httpClient;
    readonly CGTraderCaptchaApi _captchaService;

    string IBaseAddressProvider.BaseAddress => CGTraderUri.Https;

    internal CGTraderApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _captchaService = new(httpClient);
    }

    #region SessionCredentials

    internal async Task<(string CSRFToken, CGTraderCaptcha Captcha)> _RequestSessionCredentialsAsync(CancellationToken cancellationToken)
    {
        try { return await _RequestSessionCredentialAsyncCore(cancellationToken); }
        catch (Exception ex)
        {
            const string errorMessage = "Couldn't request session credentials.";
            _logger.Error(ex, errorMessage); throw new Exception(errorMessage, ex);
        }
    }

    async Task<(string CSRFToken, CGTraderCaptcha Captcha)> _RequestSessionCredentialAsyncCore(CancellationToken cancellationToken)
    {
        string htmlWithSessionCredentials = (await _httpClient.GetStringAsync(
            (this as IBaseAddressProvider).Endpoint("/load-services.js"), cancellationToken)
            ).ReplaceLineEndings(string.Empty);

        string csrfToken = _ParseCsrfTokenFrom(htmlWithSessionCredentials);
        var captcha = await _captchaService._RequestCaptchaAsync(htmlWithSessionCredentials, cancellationToken);
        return (csrfToken, captcha);
    }

    static string _ParseCsrfTokenFrom(string htmlWithSessionCredentials)
    {
        if (htmlWithSessionCredentials.Contains("csrf-token"))
            return _ParseCsrfTokenCoreFrom(htmlWithSessionCredentials);
        else throw new MissingFieldException("Returned document doesn't contain CSRF token.");
    }

    static string _ParseCsrfTokenCoreFrom(string htmlWithSessionCredentials)
    {
        const string csrfTokenMetaContentKey = "meta.content = '";
        return string.Join(null,
            htmlWithSessionCredentials[(htmlWithSessionCredentials.IndexOf(csrfTokenMetaContentKey) + csrfTokenMetaContentKey.Length)..]
            .TakeWhile(c => c != '\'')
            );
    }

    #endregion

    #region Login

    internal async Task _LoginAsync(CGTraderNetworkCredential credential, CancellationToken cancellationToken)
    {
        try { await _LoginAsyncCore(credential, cancellationToken); }
        catch (HttpRequestException ex)
        {
            const string errorMessage = "Login attempt was unsuccessful.";
            _logger.Error(ex, errorMessage);
            throw new HttpRequestException(errorMessage, ex, ex.StatusCode);
        }
    }

    async Task _LoginAsyncCore(CGTraderNetworkCredential credential, CancellationToken cancellationToken)
    {
        if (credential.Captcha is null) throw new InvalidOperationException(
            $"The value of {nameof(credential.Captcha)} can't be null when trying to login."
            );

        using (var response = await _httpClient.PostAsync((this as IBaseAddressProvider).Endpoint("/users/2fa-or-login.json"),
            await credential._AsMultipartFormDataContentAsyncUsing(_captchaService, cancellationToken), cancellationToken))
        { await response._EnsureSuccessStatusCodeAsync(cancellationToken); }
    }

    #endregion

    #region ModelAssetsUpload

    #region DraftCreation

    /// <inheritdoc cref="__CreateNewModelDraftAsync(CancellationToken)"/>
    internal async Task<string> _CreateNewModelDraftAsync(CGTraderNetworkCredential credential, CancellationToken cancellationToken)
    {
        try { return await __CreateNewModelDraftAsync(credential, cancellationToken); }
        catch (Exception ex)
        {
            const string errorMessage = "New model draft couldn't be created.";
            _logger.Error(ex, errorMessage); throw new Exception(errorMessage, ex);
        }
    }

    /// <returns>ID of the newly created model draft.</returns>
    async Task<string> __CreateNewModelDraftAsync(CGTraderNetworkCredential credential, CancellationToken cancellationToken)
    {
        _httpClient.DefaultRequestHeaders._AddOrReplaceCSRFToken(
            credential.CsrfToken = await _RequestUploadInitializingCsrfTokenAsync(cancellationToken)
            );
        string modelDraftId = await _CreateNewModelDraftAsyncCore(cancellationToken);
        
        return modelDraftId;
    }

    async Task<string> _RequestUploadInitializingCsrfTokenAsync(CancellationToken cancellationToken)
    {
        string uploadInitializingCsrfToken = await _httpClient.GetStringAsync(
            (this as IBaseAddressProvider).Endpoint("/profile/upload/model"),
            cancellationToken
            );
        return _ParseUploadInititalizingCsrfTokenFrom(uploadInitializingCsrfToken);
    }

    static string _ParseUploadInititalizingCsrfTokenFrom(string uploadInitializingCsrfToken) => string.Join(null,
        uploadInitializingCsrfToken.Skip(uploadInitializingCsrfToken.IndexOf(_CsrfTokenMeta) + _CsrfTokenMeta.Length)
        .TakeWhile(c => c != '"')
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

        foreach (var preview in composite3DModel.PreviewImages.Select(preview => (preview as CGTrader3DModelPreviewImage)!))
        {
            string uploadedFileId = await _UploadModelPreviewImageAsync(preview, modelDraftId, cancellationToken);
            (composite3DModel.Metadata as CGTrader3DModelMetadata)!.UploadedPreviewImagesIDs.Add(uploadedFileId);
        }
    }

    #region ModelFileUpload

    async Task _UploadModelFileAsync(string modelFilePath, string modelDraftId, CancellationToken cancellationToken)
    {
        try { await __UploadModelFileAsync(modelFilePath, modelDraftId, cancellationToken); }
        catch (HttpRequestException ex)
        {
            string errorMessage = $"3D model file couldn't be uploaded. ({modelFilePath})";
            throw new HttpRequestException(errorMessage, ex, ex.StatusCode);
        }
    }

    async Task __UploadModelFileAsync(string modelFilePath, string modelDraftId, CancellationToken cancellationToken)
    {
        var assetUploadSessionData = await _ReserveServerSpaceAsyncFor(modelFilePath, modelDraftId, cancellationToken);
        await _UploadModelFileAsyncCore(assetUploadSessionData, cancellationToken);
    }

    async Task<CGTrader3DModelFileUploadSessionData> _ReserveServerSpaceAsyncFor(
        string modelFilePath,
        string modelDraftId,
        CancellationToken cancellationToken)
    {
        using var modelFileStream = File.OpenRead(modelFilePath);
        using var request = new HttpRequestMessage(HttpMethod.Post,
            (this as IBaseAddressProvider).Endpoint($"/profile/items/{modelDraftId}/uploads"))
        { Content = modelFileStream._ToModelFileMultipartFormDataContent() };
        
        using var response = (await _httpClient.SendAsync(request._WithHostHeader(), cancellationToken))
            .EnsureSuccessStatusCode();

        return await CGTrader3DModelAssetUploadSessionData
            ._ForModelFileAsyncFrom(response, modelFilePath, cancellationToken);
    }

    async Task _UploadModelFileAsyncCore(
        CGTrader3DModelFileUploadSessionData modelFileUploadSessionData,
        CancellationToken cancellationToken) => 
            await modelFileUploadSessionData._SendUploadRequestAsyncWtih(_httpClient, HttpMethod.Post, cancellationToken);

    #endregion

    #region ModelPreviewUpload

    async Task<string> _UploadModelPreviewImageAsync(CGTrader3DModelPreviewImage modelPreview, string modelDraftId, CancellationToken cancellationToken)
    {
        try { return await __UploadModelPreviewImageAsync(modelPreview, modelDraftId, cancellationToken); }
        catch (Exception ex)
        {
            const string errorMessage = "3D model preview image couldn't be uploaded.";
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
            await modelPreviewImageUploadSessionData._SendUploadRequestAsyncWtih(_httpClient, HttpMethod.Options, cancellationToken);

    async Task _UploadModelPreviewImageAsyncCore(
        CGTrader3DModelPreviewImageUploadSessionData modelPreviewImageUploadSessionData,
        CancellationToken cancellationToken) =>
            await modelPreviewImageUploadSessionData._SendUploadRequestAsyncWtih(_httpClient, HttpMethod.Put, cancellationToken);

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

    internal async Task _UploadModelMetadataAsync(CGTrader3DModelMetadata metadata, string modelDraftId, CancellationToken cancellationToken) =>
        await (await _httpClient.PatchAsync(
            (this as IBaseAddressProvider).Endpoint($"/profile/items/{modelDraftId}"),
            metadata._AsCGJsonContent,
            cancellationToken)
        )._EnsureSuccessStatusCodeAsync(cancellationToken); // mb make CGTrader3DModelMetadata abstract and inherit cg and printable classes from it.

    #endregion

    #region Publishing

    internal async Task _PublishModelAsync(Composite3DModel composite3DModel, string modelDraftId, CancellationToken cancellationToken) =>
        (await _httpClient.PostAsJsonAsync(
            (this as IBaseAddressProvider).Endpoint($"/profile/items/{modelDraftId}/publish"),
            new { item = new { tags = (composite3DModel.Metadata as CGTrader3DModelMetadata)!.Tags } },
            cancellationToken)
        )._EnsureSuccessStatusCodeAsync(cancellationToken);

    #endregion

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

    internal static MultipartFormDataContent _ToModelFileMultipartFormDataContent(this FileStream fileStream)
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
