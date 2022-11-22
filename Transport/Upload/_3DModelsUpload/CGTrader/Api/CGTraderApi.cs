using Common;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using NLog;
using System.Net;
using System.Net.Http.Json;
using Transport.Models;
using Transport.Upload._3DModelsUpload.CGTrader._3DModelComponents;
using Transport.Upload._3DModelsUpload.CGTrader.Captcha;

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

    #endregion

    #region CSRFToken

    internal async Task<string> _RequestCsrfTokenAsync(CancellationToken cancellationToken)
    {
        try { return await _RequestCsrfTokenAsyncCore(cancellationToken); }
        catch (Exception ex)
        {
            const string errorMessage = "CSRF token request failed.";
            _logger.Error(ex, errorMessage); throw new Exception(errorMessage, ex);
        }
    }

    async Task<string> _RequestCsrfTokenAsyncCore(CancellationToken cancellationToken)
    {
        string htmlWithSessionCredentials = await _httpClient.GetStringAsync(
            (this as IBaseAddressProvider).Endpoint("/load-services.js"),
            cancellationToken
            );
        return _ParseCsrfTokenFrom(htmlWithSessionCredentials);
    }

    static string _ParseCsrfTokenFrom(string htmlWithSessionCredentials)
    {
        var csrfTokenRegion = new Range(0, 230);
        string csrfTokenRegionContent = htmlWithSessionCredentials[csrfTokenRegion];

        if (csrfTokenRegionContent.Contains("csrf-token"))
        {
            string? csrfToken = csrfTokenRegionContent
                .Split("'", 5, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ElementAt(3);
            if (csrfToken is not null) return csrfToken;
        }

        throw new MissingFieldException("Returned document doesn't contain CSRF token.");
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
        { await response._EnsureSuccessLoginAsync(cancellationToken); }

        //using (var response = await _httpClient.GetAsync((this as IBaseAddressProvider).Endpoint("/users/login"), cancellationToken))
        //{ response.EnsureSuccessStatusCode(); }
    }

    #endregion

    #region ModelAssetsUpload

    #region DraftCreation

    /// <inheritdoc cref="__CreateNewModelDraftAsync(CancellationToken)"/>
    internal async Task<string> _CreateNewModelDraftAsync(CancellationToken cancellationToken)
    {
        try { return await __CreateNewModelDraftAsync(cancellationToken); }
        catch (Exception ex)
        {
            const string errorMessage = "New model draft couldn't be created.";
            _logger.Error(ex, errorMessage); throw new Exception(errorMessage, ex);
        }
    }

    /// <returns>ID of the newly created model draft.</returns>
    async Task<string> __CreateNewModelDraftAsync(CancellationToken cancellationToken)
    {
        //await _InitializeUploadSessionAsync(cancellationToken);
        //await _SwitchProtocolAsync(cancellationToken);
        string modelDraftId = await _CreateNewModelDraftAsyncCore(cancellationToken);
        await _IdentifyUserAsync(cancellationToken);

        return modelDraftId;
    }

    async Task _InitializeUploadSessionAsync(CancellationToken cancellationToken) =>
        (await _httpClient.GetAsync(
            (this as IBaseAddressProvider).Endpoint("/profile/upload/model"), cancellationToken)
        ).EnsureSuccessStatusCode();

    async Task _SwitchProtocolAsync(CancellationToken cancellationToken) =>
        (await _httpClient.GetAsync(
            (this as IBaseAddressProvider).Endpoint("/cable"))
        ).EnsureSuccessStatusCode();

    async Task<string> _CreateNewModelDraftAsyncCore(CancellationToken cancellationToken)
    {
        string response = await _httpClient.GetStringAsync(
            (this as IBaseAddressProvider).Endpoint($"/api/internal/items/current-draft/cg?nocache={CaptchaRequestArguments.rt}"),
            cancellationToken
            );
        return (string)JObject.Parse(response)["data"]!["id"]!;
    }

    async Task _IdentifyUserAsync(CancellationToken cancellationToken)
    {
        string requestUri = QueryHelpers.AddQueryString("https://cgtrader.zendesk.com/embeddable_identify", new Dictionary<string, string>()
        {
            { "type", "user" },
            { "data", Guid.NewGuid().ToString() }
        });
        (await _httpClient.GetAsync(requestUri)).EnsureSuccessStatusCode();
    }

    #endregion

    #region Upload

    internal async Task _UploadModelAssetsAsyncOf(Composite3DModel composite3DModel, string modelDraftId, CancellationToken cancellationToken)
    {
        foreach (var _3DModel in composite3DModel._3DModels)
            foreach (var modelPart in _3DModel.Files)
                await _UploadModelFileAsync(modelPart, modelDraftId, cancellationToken);

        foreach (var preview in composite3DModel.PreviewImages)
            await _UploadModelPreviewImageAsync(preview, modelDraftId, cancellationToken);
    }

    #region ModelFileUpload

    async Task _UploadModelFileAsync(string filePath, string modelDraftId, CancellationToken cancellationToken)
    {
        try { await __UploadModelFileAsync(filePath, modelDraftId, cancellationToken); }
        catch (HttpRequestException ex)
        {
            string errorMessage = $"Model file couldn't be uploaded. ({filePath})";
            throw new HttpRequestException(errorMessage, ex, ex.StatusCode);
        }
    }

    async Task __UploadModelFileAsync(string filePath, string modelDraftId, CancellationToken cancellationToken)
    {
        var modelFileUploadSessionData = await _RequestModelFileUploadSessionDataAsyncFor(
            File.OpenRead(filePath),
            modelDraftId,
            cancellationToken);

        await _UploadModelFileAsyncCore(modelFileUploadSessionData, cancellationToken);

        //// edit file metadata.
        //var response = await _httpClient.PutAsJsonAsync((this as IBaseAddressProvider).Endpoint($"/api/internal/items/{modelDraftId}/item_files/{(int)responseJson["id"]!}"), new
        //{
        //    key = $"uploads/files/{modelDraftId}/{fileName}",
        //    filename = fileName,
        //    filesize = modelFileStream.Length
        //}, cancellationToken);
        //response.EnsureSuccessStatusCode();
    }

    async Task<CGTrader3DModelFileUploadSessionData> _RequestModelFileUploadSessionDataAsyncFor(
        FileStream modelFileStream,
        string modelDraftId,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync(
            (this as IBaseAddressProvider).Endpoint($"/profile/items/{modelDraftId}/uploads"),
            modelFileStream._ToModelFileMultipartFormDataContent(),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var fileUploadSessionDataJson = JObject.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var fileUploadReceiverDataJson = fileUploadSessionDataJson["storage"]!;

        return new CGTrader3DModelFileUploadSessionData(
            modelFileStream, modelDraftId,
            (string)fileUploadSessionDataJson["id"]!,
            (string)fileUploadSessionDataJson["storageLocation"]!,
            (string)fileUploadReceiverDataJson["key"]!,
            (string)fileUploadReceiverDataJson["awsAccessKeyId"]!,
            (string)fileUploadReceiverDataJson["acl"]!,
            (string)fileUploadReceiverDataJson["policy"]!,
            (string)fileUploadReceiverDataJson["signature"]!,
            (HttpStatusCode)(int)fileUploadReceiverDataJson["success_action_status"]!);
    }

    async Task _UploadModelFileAsyncCore(CGTrader3DModelFileUploadSessionData fileUploadSessionData, CancellationToken cancellationToken) =>
        (await _httpClient.PostAsync(
            fileUploadSessionData.StorageHost,
            fileUploadSessionData._AsMultipartFormDataContent,
            cancellationToken)
        ).EnsureSuccessStatusCode();

    #endregion

    #region ModelPreviewUpload

    async Task _UploadModelPreviewImageAsync(
        string filePath,
        string modelDraftId,
        CancellationToken cancellationToken) => await _UploadModelPreviewImageAsync(
            new(File.OpenRead(filePath), modelDraftId), cancellationToken
            );

    async Task _UploadModelPreviewImageAsync(CGTrader3DModelPreviewImage modelPreview, CancellationToken cancellationToken)
    {
        try { await _UploadModelPreviewImageAsyncCore(modelPreview, cancellationToken); }
        catch (Exception ex)
        {
            const string errorMessage = "Model preview couldn't be uploaded.";
            _logger.Error(ex, errorMessage); throw new Exception(errorMessage, ex);
        }
    }

    async Task _UploadModelPreviewImageAsyncCore(CGTrader3DModelPreviewImage modelPreview, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync((this as IBaseAddressProvider).Endpoint("/api/internal/direct-uploads/item-images"), new
        {
            blob = new
            {
                checksum = await modelPreview.ChecksumAsync(cancellationToken),
                filename = modelPreview.FileName,
                content_type = modelPreview.MimeType.MediaType,
                byte_size = modelPreview.FileStream.Length
            }
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var uploadedPreviewImageAttributesJson = JObject.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken)
            )["data"]!["attributes"]!;

        modelPreview.SignedFileID = (string)uploadedPreviewImageAttributesJson["signedBlobId"]!;
        modelPreview.LocationOnServer = (string)uploadedPreviewImageAttributesJson["url"]!;
    }

    // Deprecated but contains extra parameters in query string so maybe that's why I get 422 now when I don't pass it as the argument anymore,
    internal async Task _UploadModelPreviewImageAsyncCore(string filePath, int modelDraftId, CancellationToken cancellationToken)
    {
        string fileName = Path.GetFileName(filePath);
        byte[] fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        long fileLength; using (var fileStream = File.OpenRead(filePath)) { fileLength = fileStream.Length; }

        var response = await _httpClient.PostAsJsonAsync((this as IBaseAddressProvider).Endpoint("/api/internal/direct-uploads/item-images"), new
        {
            blob = new
            {
                checksum = Convert.ToBase64String(fileBytes),
                filename = fileName,
                content_type = MimeTypes.GetMimeType(fileName),
                byte_size = fileLength
            }
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseJson = JObject.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        int signedBlobId = (int)responseJson["data"]!["attributes"]!["signedBlobId"]!;
        response = await _httpClient.PutAsJsonAsync((this as IBaseAddressProvider).Endpoint($"/api/internal/direct-uploads/item-images/{signedBlobId}"), new
        {
            key = $"uploads/files/{modelDraftId}/{fileName}",
            filename = fileName,
            filesize = fileLength
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    #endregion

    #endregion

    #endregion
}

static class Extensions
{
    internal static async Task _EnsureSuccessLoginAsync(this HttpResponseMessage response, CancellationToken cancellationToken)
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
        return new MultipartFormDataContent() { { filename, "\"filename\"" }, { type, "\"type\"" } };
    }
}
