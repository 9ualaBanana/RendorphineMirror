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

    public string BaseAddress { get; init; } = "https://auth.turbosquid.com/";

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

    internal async Task _LoginAsyncUsing(TurboSquidNetworkCredential credential, CancellationToken cancellationToken)
    {
        try
        {
            await _LoginAsyncCoreUsing(credential, cancellationToken);
            _logger.Debug("{User} is successfully logged in.", credential.UserName);
        }
        catch (Exception ex)
        {
            string errorMessage = $"Login attempt for {credential.UserName} was unsuccessful.";
            _logger.Error(ex, errorMessage);
            throw new Exception(errorMessage, ex);
        }
    }

    async Task _LoginAsyncCoreUsing(TurboSquidNetworkCredential credential, CancellationToken cancellationToken)
    {
        var loginResponse = await _socketsHttpHandler.CookieContainer._RememberCookieAndRemoveAsyncAfter(
            async () => await _SignInAsync(credential, cancellationToken),
            "_keymaster_session");

        if (_IsRedirectTo2FA(loginResponse)) await _SignInWith2FAAsync(credential, cancellationToken);
        else await loginResponse._FollowRedirectWith(_httpClient, cancellationToken);
    }

    async Task<HttpResponseMessage> _SignInAsync(TurboSquidNetworkCredential credential, CancellationToken cancellationToken) =>
        (await _noAutoRedirectHttpClient.PostAsync(
            (this as IBaseAddressProvider).Endpoint("/users/sign_in?locale=en"),
            credential._ToLoginMultipartFormData(),
            cancellationToken))
        ._EnsureRedirectStatusCode();

    async Task _SignInWith2FAAsync(TurboSquidNetworkCredential credential, CancellationToken cancellationToken)
    {
        var _2faForm = await _Get2FAFormAsync(cancellationToken);
        var updatedCsrfToken = CsrfToken._ParseFromMetaTag(await _2faForm.Content.ReadAsStringAsync(cancellationToken));
        string verificationCode = (await NodeGui.Request<string>(new InputRequest("TODO: we send you mesag to email please respond"), cancellationToken)).Result;

        await _SignInWith2FAAsyncCore(verificationCode, credential._WithUpdatedCsrfToken(updatedCsrfToken), cancellationToken);
    }

    async Task<HttpResponseMessage> _Get2FAFormAsync(CancellationToken cancellationToken) =>
        (await _httpClient.GetAsync(
            (this as IBaseAddressProvider).Endpoint("/users/two_factor_authentication?locale=en"),
            cancellationToken))
        .EnsureSuccessStatusCode();

    async Task _SignInWith2FAAsyncCore(string verificationCode, TurboSquidNetworkCredential credential, CancellationToken cancellationToken)
    {
        var redirectingResponse = await _Send2FAVerificationCodeFromEmailAsync(verificationCode, credential, cancellationToken);
        redirectingResponse = (await redirectingResponse._FollowRedirectWith(_noAutoRedirectHttpClient, cancellationToken))
            ._EnsureRedirectStatusCode();

        redirectingResponse = (await _socketsHttpHandler.CookieContainer._RememberCookieAndRemoveAsyncAfter(
            async () => await redirectingResponse._FollowRedirectWith(_noAutoRedirectHttpClient, cancellationToken),
            "_turbosquid_artist_session"))
        ._EnsureRedirectStatusCode();

        (await redirectingResponse._FollowRedirectWith(_httpClient, cancellationToken)).EnsureSuccessStatusCode();
    }

    async Task<HttpResponseMessage> _Send2FAVerificationCodeFromEmailAsync(
        string verificationCode,
        TurboSquidNetworkCredential credential,
        CancellationToken cancellationToken) => (await _noAutoRedirectHttpClient.PostAsync(
            (this as IBaseAddressProvider).Endpoint("/users/two_factor_authentication.user?locale=en"),
            credential._To2FAFormUrlEncodedContentWith(verificationCode),
            cancellationToken))
        ._EnsureRedirectStatusCode();

    #region Helpers

    bool _IsRedirectTo2FA(HttpResponseMessage response) =>
        response.Headers.Location!.AbsoluteUri.StartsWith((this as IBaseAddressProvider).Endpoint("/users/two_factor_authentication"));

    #endregion
}

static class RedirectHttpResponseMessageExtensions
{
    internal static HttpResponseMessage _EnsureRedirectStatusCode(this HttpResponseMessage response)
    {
        if ((int)response.StatusCode < 300 || (int)response.StatusCode >= 400)
            throw new HttpRequestException("Response status code does not indicate redirect:", inner: null, response.StatusCode);
        else return response;
    }

    internal static async Task<HttpResponseMessage> _FollowRedirectWith(
        this HttpResponseMessage response,
        HttpClient httpClient,
        CancellationToken cancellationToken) => await httpClient.GetAsync(response.Headers.Location!, cancellationToken);

    /// <summary>
    /// Saves the reference to the <see cref="Cookie"/> with <paramref name="cookieName"/> stored inside <paramref name="cookieContainer"/>
    /// and removes it after sending the <paramref name="request"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Cookie"/>s for which this method is called must be manually deleted after the <paramref name="request"/>
    /// because <see cref="HttpClient"/> behaves inconsistently when managing (updating) cookies that were manually added 
    /// via <see cref="SocketsHttpHandler.CookieContainer.Add(Cookie)"/>. It results in 2 same cookies that differ only in their value.
    /// Following responses that set that cookie will update the value of the one that was added automatically by <see cref="HttpClient"/>,
    /// the one added manually will be unreachable by <see cref="HttpClient"/> and particularly responses that should update
    /// that cookie (they will update the value of the cookie that was set automatically by <see cref="HttpClient"/>).
    /// </remarks>
    /// <param name="cookieContainer">
    /// The container from which <paramref name="cookieName"/> should be removed after sending the <paramref name="request"/>.
    /// </param>
    /// <param name="request">The request after which the cookie with <paramref name="cookieName"/> should be removed.</param>
    /// <param name="cookieName">The name of the cookie that should be removed after the <paramref name="request"/>.</param>
    /// <returns>The response received from the <paramref name="request"/>.</returns>
    internal static async Task<HttpResponseMessage> _RememberCookieAndRemoveAsyncAfter(
        this CookieContainer cookieContainer,
        Func<Task<HttpResponseMessage>> request,
        string cookieName)
    {
        var cookieToRemove = cookieContainer.GetAllCookies().First(cookie => cookie.Name == cookieName);
        var response = await request();
        cookieToRemove.Expired = true;
        return response;
    }
}
