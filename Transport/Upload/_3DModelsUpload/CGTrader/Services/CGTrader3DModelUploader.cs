﻿using System.Net.Http.Headers;
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
        if (credential.CsrfToken is null)
        {
            (credential.CsrfToken, credential.Captcha) = await _api._RequestSessionCredentialsAsync(cancellationToken);
            HttpClient.DefaultRequestHeaders._AddOrReplaceCSRFToken(credential.CsrfToken);
        }

        await _api._LoginAsync(credential, cancellationToken);
        int modelDraftId = await _api._CreateNewModelDraftAsync(cancellationToken);
        await _api._UploadModelFilesAsyncOf(composite3DModel, modelDraftId, cancellationToken);
    }
}

static class HttpRequestHeadersExtensions
{
    internal static void _AddOrReplaceCSRFToken(this HttpRequestHeaders headers, string csrfToken)
    {
        const string Header = "X-CSRF-Token";

        headers.Remove(Header);
        headers.Add(Header, csrfToken);
    }
}
