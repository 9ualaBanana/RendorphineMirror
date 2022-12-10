using System.Net;
using System.Security;
using Transport.Upload._3DModelsUpload.Turbosquid.Api;

namespace Transport.Upload._3DModelsUpload.Turbosquid.Network.Authenticity;

internal class TurboSquidNetworkCredential : NetworkCredential
{
    internal readonly string _CsrfToken;
    internal readonly string _CaptchaVerifiedToken;
    internal readonly string _ApplicationUserID;
    internal MultipartFormDataContent _AsMultipartFormData => _asMultipartFormData ??= new()
    {
        { new StringContent("✓"), "utf8" },
        { new StringContent(_CsrfToken), "authenticity_token" },
        { new StringContent(_CaptchaVerifiedToken), "g-recaptcha-response-data[login]" },
        { new StringContent(string.Empty), "g-recaptcha-response-data" },
        { new StringContent(UserName), "user[email]" },
        { new StringContent(_ApplicationUserID), "user[application_uid]" },
        { new StringContent(Password), "user[password]" }
    };
    MultipartFormDataContent? _asMultipartFormData;

    internal static ForeignThreadValue<string> _ServerResponse = new(false);

    internal TurboSquidNetworkCredential(
        NetworkCredential credential,
        string csrfToken,
        string applicationUserId,
        string captchaVerifiedToken)
        : this(credential.UserName, credential.Password, csrfToken, applicationUserId, captchaVerifiedToken)
    {
    }

    internal TurboSquidNetworkCredential(
        string userName,
        string password,
        string csrfToken,
        string applicationUserId,
        string captchaVerifiedToken) : base(userName, password)
    {
        _CsrfToken = csrfToken;
        _CaptchaVerifiedToken = captchaVerifiedToken;
        _ApplicationUserID = applicationUserId;
    }

    internal TurboSquidNetworkCredential(
        string userName,
        SecureString password,
        string csrfToken,
        string applicationUserId,
        string captchaVerifiedToken) : base(userName, password)
    {
        _CsrfToken = csrfToken;
        _CaptchaVerifiedToken = captchaVerifiedToken;
        _ApplicationUserID = applicationUserId;
    }

    internal static async Task<TurboSquidNetworkCredential> _RequestAsyncUsing(
        TurboSquidApi api,
        NetworkCredential credential,
        CancellationToken cancellationToken) => await api._RequestTurboSquidNetworkCredentialAsync(credential, cancellationToken);
}
