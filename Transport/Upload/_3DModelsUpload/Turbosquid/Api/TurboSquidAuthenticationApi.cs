using NLog;
using NodeToUI;
using NodeToUI.Requests;
using System.Net;
using Transport.Models;
using Transport.Upload._3DModelsUpload.Turbosquid.Network.Authenticity;

namespace Transport.Upload._3DModelsUpload.Turbosquid.Api;

internal class TurboSquidAuthenticationApi : IBaseAddressProvider
{
    readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    readonly SocketsHttpHandler _socketsHttpHandler;
    readonly HttpClient _httpClient;
    readonly HttpClient _noAutoRedirectHttpClient;

    public string BaseAddress => "https://auth.turbosquid.com/";

    internal TurboSquidAuthenticationApi(SocketsHttpHandler socketsHttpHandler)
    {
        _socketsHttpHandler = socketsHttpHandler;
        _httpClient = new(socketsHttpHandler);
        _noAutoRedirectHttpClient = new HttpClient(new SocketsHttpHandler()
        {
            AllowAutoRedirect = false,
            CookieContainer = socketsHttpHandler.CookieContainer
        });
    }

    internal async Task _LoginAsync(TurboSquidNetworkCredential credential, CancellationToken cancellationToken)
    {
        try
        {
            await _LoginAsyncCore(credential, cancellationToken);
            _logger.Debug("{User} is successfully logged in.", credential.UserName);
        }
        catch (Exception ex)
        {
            string errorMessage = $"Login attempt for {credential.UserName} was unsuccessful.";
            _logger.Error(ex, errorMessage);
            throw new Exception(errorMessage, ex);
        }
    }

    async Task _LoginAsyncCore(TurboSquidNetworkCredential credential, CancellationToken cancellationToken)
    {
        // Must be manually deleted after the following request because HttpClient behaves inconsistently when managing (updating) cookies that were manually added via SocketHttpHandler.CookieContainer.Add(Cookie).
        // It results in 2 same cookies that differ only in their value. Following responses that set that cookie will update the value of the one that was added automatically by HttpClient,
        // the one added manually will be unreachable by HttpClient and particularly responses that should update that cookie (they will update the value of the cookie that was set automatically by HttpClient).
        var staleCookie = _socketsHttpHandler.CookieContainer.GetAllCookies().Cast<Cookie>().Single(cookie => cookie.Name == "_keymaster_session");
        // TODO: implement abstraction for removing stale cookies.

        var loginResponse = await _SignInAsync(credential, cancellationToken);
        if (_IsRedirectTo2FA(loginResponse))
        {
            staleCookie.Expired = true;
            await _SignInWith2FA(credential, cancellationToken);
        }
    }

    async Task _SignInWith2FA(TurboSquidNetworkCredential credential, CancellationToken cancellationToken)
    {
        var _2faForm = await _Get2FAFormAsync(cancellationToken);
        credential._CsrfToken = CsrfToken._ParseFromMetaTag(await _2faForm.Content.ReadAsStringAsync(cancellationToken));
        string verificationCode = (await NodeGui.Request<string>(new InputRequest("TODO: we send you mesag to email please respond"), cancellationToken)).Result;
        await _Send2FAVerificationCodeFromEmailAsync(verificationCode, credential, cancellationToken);
    }

    async Task<HttpResponseMessage> _SignInAsync(TurboSquidNetworkCredential credential, CancellationToken cancellationToken) =>
        (await _noAutoRedirectHttpClient.PostAsync(
            (this as IBaseAddressProvider).Endpoint("/users/sign_in?locale=en"),
            credential._ToLoginMultipartFormData(),
            cancellationToken))
        .EnsureRedirectStatusCode();

    async Task<HttpResponseMessage> _Get2FAFormAsync(CancellationToken cancellationToken) =>
        (await _httpClient.GetAsync(
            (this as IBaseAddressProvider).Endpoint("/users/two_factor_authentication?locale=en"),
            cancellationToken))
        .EnsureSuccessStatusCode();


    async Task _Send2FAVerificationCodeFromEmailAsync(
        string verificationCode,
        TurboSquidNetworkCredential credential,
        CancellationToken cancellationToken) => (await _noAutoRedirectHttpClient.PostAsync(
            (this as IBaseAddressProvider).Endpoint("/users/two_factor_authentication.user?locale=en"),
            credential._To2FAFormUrlEncodedContentWith(verificationCode),
            cancellationToken))
        .EnsureRedirectStatusCode();

    #region Helpers

    bool _IsRedirectTo2FA(HttpResponseMessage response) =>
        response.Headers.Location!.AbsoluteUri.StartsWith((this as IBaseAddressProvider).Endpoint("/users/two_factor_authentication"));

    #endregion
}

static class RedirectHttpResponseMessageExtensions
{
    internal static HttpResponseMessage EnsureRedirectStatusCode(this HttpResponseMessage response)
    {
        if ((int)response.StatusCode < 300 || (int)response.StatusCode >= 400)
            throw new HttpRequestException("Response status code does not indicate redirect:", inner: null, response.StatusCode);
        else return response;
    }
}
