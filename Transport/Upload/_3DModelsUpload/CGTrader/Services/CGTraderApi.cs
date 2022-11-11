using Common;
using Newtonsoft.Json.Linq;
using System.Net.Http.Json;
using Transport.Upload._3DModelsUpload.CGTrader.Models;
using Transport.Upload._3DModelsUpload.Models;

namespace Transport.Upload._3DModelsUpload.CGTrader.Services;

internal class CGTraderApi
{
    readonly HttpClient _httpClient;
    readonly CGTraderCaptchaService _captchaService;


    internal CGTraderApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _captchaService = new(httpClient);
    }


    internal async Task<(string CSRFToken, CGTraderCaptcha Captcha)> _RequestSessionCredentialsAsync(CancellationToken cancellationToken)
    {
        string htmlWithSessionCredentials = await _httpClient.GetStringAsync(Endpoint("/load-services.js"), cancellationToken);

        string csrfToken = _ParseCsrfTokenFrom(htmlWithSessionCredentials);
        var captcha = await _captchaService._RequestCaptchaAsync(htmlWithSessionCredentials, cancellationToken);
        return (csrfToken, captcha);
    }

    #region CSRFToken
    async Task<string> _RequestCsrfTokenAsync(CancellationToken cancellationToken)
    {
        string htmlWithSessionCredentials = await _httpClient.GetStringAsync(Endpoint("/load-services.js"), cancellationToken);
        return _ParseCsrfTokenFrom(htmlWithSessionCredentials);
    }

    static string _ParseCsrfTokenFrom(string htmlWithSessionCredentials)
    {
        var csrfTokenRegion = new Range(0, 230);
        string csrfTokenRegionContent = htmlWithSessionCredentials[csrfTokenRegion];

        if (csrfTokenRegionContent.Contains("csrf-token"))
        {
            string? csrfToken = csrfTokenRegionContent
                .ReplaceLineEndings(string.Empty)
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
        { /* Add logging. */ throw new HttpRequestException("Login attempt was unsuccessful.", ex, ex.StatusCode); }
    }

    async Task _LoginAsyncCore(CGTraderNetworkCredential credential, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync(Endpoint("/users/2fa-or-login.json"),
            credential._AsMultipartFormDataContent, cancellationToken);
        await response._EnsureSuccessLoginAsync(cancellationToken);
    }
    #endregion

    /// <returns>ID of the newly created model draft.</returns>
    internal async Task<int> _CreateNewModelDraftAsync(CancellationToken cancellationToken)
    {
        string response = await _httpClient.GetStringAsync(
            Endpoint($"/api/internal/items/current-draft/cg?nocache={CGTraderRequestArguments.rt}"), cancellationToken);
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
        { /* Add logging */ throw new HttpRequestException($"Model file couldn't be uploaded. ({filePath})", ex, ex.StatusCode); }
    }

    async Task _UploadModelFileAsyncCore(string filePath, int modelDraftId, CancellationToken cancellationToken)
    {
        using var modelFileStream = File.OpenRead(filePath);
        var response = await _httpClient.PostAsync(Endpoint($"/profile/items/{modelDraftId}/uploads"),
            modelFileStream._ToModelFileMultipartFormDataContent(), cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseJson = JObject.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        string fileName = Path.GetFileName(filePath);
        response = await _httpClient.PutAsJsonAsync(Endpoint($"/api/internal/items/{modelDraftId}/item_files/{(int)responseJson["id"]!}"), new
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

        var response = await _httpClient.PostAsJsonAsync(Endpoint("/api/internal/direct-uploads/item-images"), new
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
        response = await _httpClient.PutAsJsonAsync(Endpoint($"/api/internal/direct-uploads/item-images/{signedBlobId}"), new
        {
            key = $"uploads/files/{modelDraftId}/{fileName}",
            filename = fileName,
            filesize = fileLength
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    static string Endpoint(string endpointWithoutDomain)
    {
        if (!endpointWithoutDomain.StartsWith('/'))
            endpointWithoutDomain = '/' + endpointWithoutDomain;
        
        return $"{CGTraderUri.Https}{endpointWithoutDomain}";
    }
}
