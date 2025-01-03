﻿using Transport.Upload._3DModelsUpload.CGTrader.Api;
using Transport.Upload._3DModelsUpload.CGTrader.Network.Captcha;

namespace Transport.Upload._3DModelsUpload.CGTrader.Network;

internal record CGTraderSessionContext
{
    internal readonly CGTraderNetworkCredential Credential;
    internal string CsrfToken;
    internal readonly CGTraderCaptcha Captcha;

    internal CGTraderSessionContext(
        CGTraderNetworkCredential credential,
        string csrfToken,
        CGTraderCaptcha captcha)
    {
        Credential = credential;
        CsrfToken = csrfToken;
        Captcha = captcha;
    }

    internal static async Task<CGTraderSessionContext> CreateAsyncUsing(
        CGTraderApi api,
        CGTraderNetworkCredential credential,
        CancellationToken cancellationToken
        ) => await api._RequestSessionContextAsync(credential, cancellationToken);

    internal MultipartFormDataContent _LoginMultipartFormDataWith(string captchaSolution)
    {
        return _asMultipartFormDataContent ??= new()
        {
            { new StringContent(CsrfToken), "authenticity_token" },
            { new StringContent(captchaSolution), "user[MTCaptchaToken]" },
            { new StringContent("/"), "location" },
            { new StringContent(Credential.UserName), "user[login]" },
            { new StringContent(Credential.Password), "user[password]" },
            { new StringContent(Credential._RememberMe), "user[remember_me]" },
            { new StringContent(captchaSolution), "mtcaptcha-verifiedtoken" }
        };
    }
    MultipartFormDataContent? _asMultipartFormDataContent;
}
