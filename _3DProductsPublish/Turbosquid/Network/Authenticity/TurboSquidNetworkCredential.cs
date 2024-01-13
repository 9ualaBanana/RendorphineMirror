using System.Net;
using System.Security;

namespace _3DProductsPublish.Turbosquid.Network.Authenticity;

public class TurboSquidNetworkCredential : NetworkCredential
{
    internal readonly string _CsrfToken;
    internal readonly string _CaptchaVerifiedToken;
    internal readonly string _ApplicationUserID;

    #region Initialization

    internal static ForeignThreadValue<string> Response = new(false);

    internal TurboSquidNetworkCredential WithUpdated(string csrfToken) =>
        new(UserName, Password, csrfToken, _ApplicationUserID, _CaptchaVerifiedToken);

    internal TurboSquidNetworkCredential(
        NetworkCredential credential,
        string csrfToken,
        string applicationUserId,
        string captchaVerifiedToken)
        : this(credential.UserName, credential.Password, csrfToken, applicationUserId, captchaVerifiedToken)
    {
    }

    TurboSquidNetworkCredential(
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

    #endregion

    internal MultipartFormDataContent _ToLoginMultipartFormData() => new()
    {
        { new StringContent("✓"), "utf8" },
        { new StringContent(_CsrfToken), "authenticity_token" },
        { new StringContent(_CaptchaVerifiedToken), "g-recaptcha-response-data[login]" },
        { new StringContent(string.Empty), "g-recaptcha-response-data" },
        { new StringContent(UserName), "user[email]" },
        { new StringContent(_ApplicationUserID), "user[application_uid]" },
        { new StringContent(Password), "user[password]" }
    };

    internal FormUrlEncodedContent _To2FAFormUrlEncodedContentWith(string emailVerificationCode) =>
        new(new Dictionary<string, string>()
        {
            { "utf8", "✓" },
            { "_method", "put" },
            { "authenticity_token" , _CsrfToken },
            { "code", emailVerificationCode },
            { "application_uid", _ApplicationUserID },
            { "commit", "Submit" }
        });
}
