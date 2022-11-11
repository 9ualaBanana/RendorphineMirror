using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using Transport.Upload._3DModelsUpload.CGTrader.Models;
using Transport.Upload._3DModelsUpload.Models;
using Transport.Upload._3DModelsUpload.Services;

namespace Transport.Upload._3DModelsUpload.CGTrader.Services;

internal class CGTrader3DModelUploader : _3DModelUploaderBase<CGTrader3DModelMetadata>
{
    readonly CGTraderApi _api;

    internal CGTrader3DModelUploader(HttpClient httpClient) : base(httpClient)
    {
        _api = new(httpClient);
    }


    internal override async Task UploadAsync(
        CGTraderNetworkCredential credential,
        Composite3DModel composite3DModel,
        CGTrader3DModelMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        if (credential.CSRFToken is null)
        {
            (credential.CSRFToken, credential.Captcha) = await _api._RequestSessionCredentialsAsync(cancellationToken);
            HttpClient.DefaultRequestHeaders._AddOrReplaceCSRFToken(credential.CSRFToken);
        }

        await _api._LoginAsync(credential, cancellationToken);
        int modelDraftId = await _api._CreateNewModelDraftAsync(cancellationToken);
        await _api._UploadModelFilesAsyncOf(composite3DModel, modelDraftId, cancellationToken);
    }
}

static class HttpExtensions
{
    internal static void _AddOrReplaceCSRFToken(this HttpRequestHeaders headers, string csrfToken)
    {
        const string Header = "X-CSRF-Token";

        headers.Remove(Header);
        headers.Add(Header, csrfToken);
    }

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
