﻿using Common;
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
        string htmlWithSessionCredentials = (await _httpClient.GetStringAsync(
            _Endpoint("/load-services.js"), cancellationToken)
            ).ReplaceLineEndings(string.Empty);

        string csrfToken = _ParseCsrfTokenFrom(htmlWithSessionCredentials);
        var captcha = await _captchaService._RequestCaptchaAsync(htmlWithSessionCredentials, cancellationToken);
        return (csrfToken, captcha);
    }

    #region CSRFToken
    internal async Task<string> _RequestCsrfTokenAsync(CancellationToken cancellationToken)
    {
        string htmlWithSessionCredentials = await _httpClient.GetStringAsync(_Endpoint("/load-services.js"), cancellationToken);
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
        { /* Add logging. */ throw new HttpRequestException("Login attempt was unsuccessful.", ex, ex.StatusCode); }
    }

    async Task _LoginAsyncCore(CGTraderNetworkCredential credential, CancellationToken cancellationToken)
    {
        await credential.Captcha._SolveAsyncUsing(_captchaService, cancellationToken);
        using var response = await _httpClient.PostAsync(_Endpoint("/users/2fa-or-login.json"),
            await credential._ToMultipartFormDataContentAsyncUsing(_captchaService, cancellationToken),
            cancellationToken
            );
        await response._EnsureSuccessLoginAsync(cancellationToken);
    }
    #endregion

    /// <returns>ID of the newly created model draft.</returns>
    internal async Task<int> _CreateNewModelDraftAsync(CancellationToken cancellationToken)
    {
        string response = await _httpClient.GetStringAsync(
            _Endpoint($"/api/internal/items/current-draft/cg?nocache={CaptchaRequestArguments.rt}"), cancellationToken);
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
        var response = await _httpClient.PostAsync(_Endpoint($"/profile/items/{modelDraftId}/uploads"),
            modelFileStream._ToModelFileMultipartFormDataContent(), cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseJson = JObject.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        string fileName = Path.GetFileName(filePath);
        response = await _httpClient.PutAsJsonAsync(_Endpoint($"/api/internal/items/{modelDraftId}/item_files/{(int)responseJson["id"]!}"), new
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

        var response = await _httpClient.PostAsJsonAsync(_Endpoint("/api/internal/direct-uploads/item-images"), new
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
        response = await _httpClient.PutAsJsonAsync(_Endpoint($"/api/internal/direct-uploads/item-images/{signedBlobId}"), new
        {
            key = $"uploads/files/{modelDraftId}/{fileName}",
            filename = fileName,
            filesize = fileLength
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    static string _Endpoint(string endpointWithoutDomain)
    {
        if (!endpointWithoutDomain.StartsWith('/'))
            endpointWithoutDomain = '/' + endpointWithoutDomain;
        
        return $"{CGTraderUri.Https}{endpointWithoutDomain}";
    }
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