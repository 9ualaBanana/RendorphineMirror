using System.Net;
using System.Security;

namespace Transport.Upload._3DModelsUpload.CGTrader.Models;

public class CGTraderNetworkCredential : NetworkCredential
{
    public string? CSRFToken { get; internal set; }
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


    internal MultipartFormDataContent _AsMultipartFormDataContent
    {
        get
        {
            if (CSRFToken is null)
                throw new InvalidOperationException($"{nameof(CSRFToken)} can't be null when calling {nameof(_AsMultipartFormDataContent)}.");
            if (Captcha is null)
                throw new InvalidOperationException($"{nameof(Captcha)} can't be null when caling {nameof(_AsMultipartFormDataContent)}.");


            return _asMultipartFormData ??= new()
            {
                { new StringContent(CSRFToken), "authenticity_token" },
                { new StringContent(Captcha._Solve()), "user[MTCaptchaToken]" },
                { new StringContent("/"), "location" },
                { new StringContent(UserName), "user[login]" },
                { new StringContent(Password), "user[password]" },
                { new StringContent(_RememberMe), "user[remember_me]" }
            };
        }
    }
    MultipartFormDataContent? _asMultipartFormData;
}
