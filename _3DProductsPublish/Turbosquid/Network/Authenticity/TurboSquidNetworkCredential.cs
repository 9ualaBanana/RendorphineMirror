using System.Net;
using System.Security;

namespace _3DProductsPublish.Turbosquid.Network.Authenticity;

public class TurboSquidNetworkCredential : NetworkCredential
{
    internal string AuthenticityToken { get; private set; }
    internal readonly string _CaptchaVerifiedToken;
    internal readonly string _ApplicationUserID;

    #region Initialization

    internal static ForeignThreadValue<string> Response = new(false);


    internal void Update(string authenticityToken) => WithUpdated(authenticityToken);
    internal TurboSquidNetworkCredential WithUpdated(string authenticityToken)
    { AuthenticityToken = authenticityToken; return this; }

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
        AuthenticityToken = csrfToken;
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
        AuthenticityToken = csrfToken;
        _CaptchaVerifiedToken = captchaVerifiedToken;
        _ApplicationUserID = applicationUserId;
    }

    #endregion

    internal MultipartFormDataContent _ToLoginMultipartFormData() => new()
    {
        { new StringContent("✓"), "utf8" },
        { new StringContent(AuthenticityToken), "authenticity_token" },
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
            { "authenticity_token" , AuthenticityToken },
            { "code", emailVerificationCode },
            { "application_uid", _ApplicationUserID },
            { "commit", "Submit" }
        });
}
