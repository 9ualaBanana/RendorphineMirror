using Common;
using Newtonsoft.Json.Linq;
using NLog;
using System.Net.Http;
using System.Net.Http.Json;
using Transport.Models;
using Transport.Upload._3DModelsUpload.CGTrader.Models;
using Transport.Upload._3DModelsUpload.Models;

namespace Transport.Upload._3DModelsUpload.CGTrader.Services;

internal class CGTraderApi : IBaseAddressProvider
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    readonly HttpClient _httpClient;
    readonly CGTraderCaptchaService _captchaService;

    string IBaseAddressProvider.BaseAddress => CGTraderUri.Https;

    internal CGTraderApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _captchaService = new(httpClient);
    }


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
        string htmlWithSessionCredentials = await _httpClient.GetStringAsync((this as IBaseAddressProvider).Endpoint("/load-services.js"), cancellationToken);
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

        using var response = await _httpClient.PostAsync((this as IBaseAddressProvider).Endpoint("/users/2fa-or-login.json"),
            await credential._AsMultipartFormDataContentAsyncUsing(_captchaService, cancellationToken),
            cancellationToken
            );
        await response._EnsureSuccessLoginAsync(cancellationToken);
    }

    #endregion

    #region ModelFilesUpload

    /// <returns>ID of the newly created model draft.</returns>
    internal async Task<int> _CreateNewModelDraftAsync(CancellationToken cancellationToken)
    {
        try { return await _CreateNewModelDraftAsyncCore(cancellationToken); }
        catch (Exception ex)
        {
            const string errorMessage = "New model draft couldn't be created.";
            _logger.Error(ex, errorMessage); throw new Exception(errorMessage, ex);
        }
    }

    async Task<int> _CreateNewModelDraftAsyncCore(CancellationToken cancellationToken)
    {
        string response = await _httpClient.GetStringAsync(
            (this as IBaseAddressProvider).Endpoint($"/api/internal/items/current-draft/cg?nocache={CaptchaRequestArguments.rt}"), cancellationToken);
        var responseJson = JObject.Parse(response);

        var modelDraftId = (int)responseJson["data"]!["id"]!;
        return modelDraftId;
    }

    internal async Task _UploadModelFilesAsyncOf(Composite3DModel composite3DModel, int modelDraftId, CancellationToken cancellationToken)
    {
        foreach (var _3DModel in composite3DModel._3DModels)
            foreach (var modelPart in _3DModel.Files())
                await _UploadModelFileAsync(modelPart, modelDraftId, cancellationToken);

        foreach (var preview in composite3DModel.Previews)
            await _UploadModelPreviewImageAsyncCore(preview, modelDraftId, cancellationToken);
    }

    async Task _UploadModelFileAsync(string filePath, int modelDraftId, CancellationToken cancellationToken)
    {
        try { await _UploadModelFileAsyncCore(filePath, modelDraftId, cancellationToken); }
        catch (HttpRequestException ex)
        {
            string errorMessage = $"Model file couldn't be uploaded. ({filePath})";
            throw new HttpRequestException(errorMessage, ex, ex.StatusCode); }
    }

    async Task _UploadModelFileAsyncCore(string filePath, int modelDraftId, CancellationToken cancellationToken)
    {
        using var modelFileStream = File.OpenRead(filePath);
        var response = await _httpClient.PostAsync((this as IBaseAddressProvider).Endpoint($"/profile/items/{modelDraftId}/uploads"),
            modelFileStream._ToModelFileMultipartFormDataContent(), cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseJson = JObject.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        string fileName = Path.GetFileName(filePath);
        response = await _httpClient.PutAsJsonAsync((this as IBaseAddressProvider).Endpoint($"/api/internal/items/{modelDraftId}/item_files/{(int)responseJson["id"]!}"), new
        {
            key = $"uploads/files/{modelDraftId}/{fileName}",
            filename = fileName,
            filesize = modelFileStream.Length
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

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

    internal static MultipartFormDataContent _ToModelFileMultipartFormDataContent(this FileStream fileStream) => new()
    {
        { new StringContent(Path.GetFileName(fileStream.Name)), "filename" },
        //{ new StreamContent(fileStream), "filename", Path.GetFileName(fileStream.Name) },
        { new StringContent("file"), "type" }
    };
}
