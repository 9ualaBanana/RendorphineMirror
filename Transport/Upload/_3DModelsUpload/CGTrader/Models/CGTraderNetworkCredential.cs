using System.Net;
using System.Security;
using Transport.Upload._3DModelsUpload.CGTrader.Services;

namespace Transport.Upload._3DModelsUpload.CGTrader.Models;

public sealed class CGTraderNetworkCredential : NetworkCredential
{
    // make lateinit
    public string? CsrfToken { get; internal set; }
    // make lateinit
    public CGTraderCaptcha? Captcha { get; internal set; }
    string _RememberMe => _rememberMe ? "on" : "off";
    bool _rememberMe;


    public CGTraderNetworkCredential(string? userName, string? password, bool rememberMe)
        : base(userName, password, CGTraderUri.Domain)
    {
        _rememberMe = rememberMe;
    }

    public CGTraderNetworkCredential(string? userName, SecureString password, bool rememberMe)
        : base(userName, password, CGTraderUri.Domain)
    {
        _rememberMe = rememberMe;
    }


    internal async ValueTask<MultipartFormDataContent> _AsMultipartFormDataContentAsyncUsing(
        CGTraderCaptchaService captchaService,
        CancellationToken cancellationToken)
    {
        if (CsrfToken is null)
            throw new InvalidOperationException($"{nameof(CsrfToken)} can't be null when calling {nameof(_AsMultipartFormDataContentAsyncUsing)}.");
        if (Captcha is null)
            throw new InvalidOperationException($"{nameof(Captcha)} can't be null when caling {nameof(_AsMultipartFormDataContentAsyncUsing)}.");


        return _asMultipartFormData ??= new()
        {
            { new StringContent(CsrfToken), "authenticity_token" },
            { new StringContent(
                await Captcha._SolveAsyncUsing(captchaService, cancellationToken)),
                "user[MTCaptchaToken]"
            },
            { new StringContent("/"), "location" },
            { new StringContent(UserName), "user[login]" },
            { new StringContent(Password), "user[password]" },
            { new StringContent(_RememberMe), "user[remember_me]" },
            { new StringContent(
                await Captcha._SolveAsyncUsing(captchaService, cancellationToken)),
                "mtcaptcha-verifiedtoken"
            }
        };
    }
    MultipartFormDataContent? _asMultipartFormData;
}
