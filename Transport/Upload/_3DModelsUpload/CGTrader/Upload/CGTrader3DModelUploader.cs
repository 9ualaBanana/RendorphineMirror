using System.Net.Http.Headers;
using Transport.Upload._3DModelsUpload.CGTrader._3DModelComponents;
using Transport.Upload._3DModelsUpload.CGTrader.Api;

namespace Transport.Upload._3DModelsUpload.CGTrader.Upload;

internal class CGTrader3DModelUploader : _3DModelUploaderBase
{
    readonly CGTraderApi _api;

    internal CGTrader3DModelUploader(HttpClient httpClient) : base(httpClient)
    {
        _api = new(httpClient);
    }

    internal override async Task UploadAsync(
        CGTraderNetworkCredential credential,
        Composite3DModel composite3DModel,
        CancellationToken cancellationToken = default)
    {
        if (credential.CsrfToken is null)
        {
            // Define a helper for setting CsrfToken in CGTTraderNetowrkCredential and HttpClient.DefaultRequestHeaders
            (credential.CsrfToken, credential.Captcha) = await _api._RequestSessionCredentialsAsync(cancellationToken);
            HttpClient.DefaultRequestHeaders._AddOrReplaceCSRFToken(credential.CsrfToken);
        }

        await _api._LoginAsync(credential, cancellationToken);
        string modelDraftId = await _api._CreateNewModelDraftAsync(credential, cancellationToken);
        _UpcastPreviewImagesOf(composite3DModel);
        await _api._UploadModelAssetsAsyncOf(composite3DModel, modelDraftId, cancellationToken);
        await _api._UploadModelMetadataAsync((composite3DModel.Metadata as CGTrader3DModelMetadata)!, modelDraftId, cancellationToken);
        await _api._PublishModelAsync(composite3DModel, modelDraftId, cancellationToken);
    }

    static void _UpcastPreviewImagesOf(Composite3DModel composite3DModel) =>
        composite3DModel.PreviewImages = composite3DModel.PreviewImages
        .Select(previewImage => new CGTrader3DModelPreviewImage(previewImage.FilePath));
}

internal static class HttpRequestHeadersExtensions
{
    internal static void _AddOrReplaceCSRFToken(this HttpRequestHeaders headers, string csrfToken)
    {
        const string Header = "X-CSRF-Token";

        headers.Remove(Header);
        headers.Add(Header, csrfToken);
    }
}
