using System.Net;
using _3DProductsPublish.Turbosquid.Network;
using _3DProductsPublish.Turbosquid.Network.Authenticity;
using CefSharp.OffScreen;
using Microsoft.Net.Http.Headers;
using _3DProductsPublish.Turbosquid.Upload;
using CefSharp;

namespace _3DProductsPublish.Turbosquid.Api;

internal class TurboSquidAuthenticationApi : IBaseAddressProvider
{
    readonly Logger _logger = LogManager.GetCurrentClassLogger();

    readonly SocketsHttpHandler _socketsHttpHandler;
    readonly HttpClient _httpClient;
    readonly HttpClient _noAutoRedirectHttpClient;
    readonly INodeGui _nodeGui;

    public string BaseAddress { get; init; } = "https://auth.turbosquid.com/";

    internal TurboSquidAuthenticationApi(SocketsHttpHandler socketsHttpHandler, INodeGui nodeGui)
    {
        _socketsHttpHandler = socketsHttpHandler;
        _httpClient = new(socketsHttpHandler);
        _noAutoRedirectHttpClient = new HttpClient(new SocketsHttpHandler()
        {
            AllowAutoRedirect = false,
            CookieContainer = socketsHttpHandler.CookieContainer
        });
        _nodeGui = nodeGui;

        _httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "gualabanana");
        _noAutoRedirectHttpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "gualabanana");
    }

    internal async Task<TurboSquidNetworkCredential> RequestTurboSquidNetworkCredentialAsync(NetworkCredential credential, CancellationToken cancellationToken)
    {
        try
        {
            var credential_ = await RequestTurboSquidNetworkCredentialAsyncCore();
            _logger.Debug("{Credential} was received.", nameof(TurboSquidNetworkCredential));
            return credential_;
        }
        catch (Exception ex)
        {
            string errorMessage = $"{nameof(TurboSquidNetworkCredential)} request failed.";
            _logger.Error(ex, errorMessage); throw new Exception(errorMessage, ex);
        }

        // I don't remember why it's implemented via synchronous thread and if it's necessary.
        async Task<TurboSquidNetworkCredential> RequestTurboSquidNetworkCredentialAsyncCore()
        {
            TurboSquidNetworkCredential credential_ = null!;
            var thread = new Thread(() =>
            {
                using var browser = new ChromiumWebBrowser(new Uri(TurboSquid.Origin, "auth/keymaster").AbsoluteUri)
                { RequestHandler = RequestHandler._ };  // Consider using ResourceRequestHandlerFactory.
                _ = browser.WaitForInitialLoadAsync().Result;

                string credentialResponse = TurboSquidNetworkCredential.Response.GetAsync(cancellationToken).Result;
                string csrfToken = AuthenticityToken.ParseFromMetaTag(credentialResponse);
                string applicationUserId = TurboSquidApplicationUserID._Parse(credentialResponse);
                string captchaVerifiedTokenResponse = TurboSquidCaptchaVerifiedToken.Response.GetAsync(cancellationToken).Result;
                string captchaVerifiedToken = TurboSquidCaptchaVerifiedToken._Parse(captchaVerifiedTokenResponse);

                browser.DumpCookiesTo(_socketsHttpHandler.CookieContainer);

                credential_ = new(credential, csrfToken, applicationUserId, captchaVerifiedToken);
            });
            thread.Start();
            await Task.Run(thread.Join, cancellationToken);

            return credential_;
        }
    }

    internal async Task<HttpResponseMessage> LoginAsync(TurboSquidNetworkCredential credential, CancellationToken cancellationToken)
    {
        try
        {
            var response = await LoginAsyncCore();
            _logger.Debug("{User} is successfully logged in.", credential.UserName);
            return response;
        }
        catch (Exception ex)
        {
            string errorMessage = $"Login attempt for {credential.UserName} was unsuccessful.";
            _logger.Error(ex, errorMessage);
            throw new Exception(errorMessage, ex);
        }


        async Task<HttpResponseMessage> LoginAsyncCore()
        {
            var loginResponse = await _socketsHttpHandler.CookieContainer
                .RememberCookieAndRemoveAsyncAfter(async () => await SignInAsync(),
                "_keymaster_session");

            return (await (_IsRedirectTo2FA(loginResponse) ? SignInWith2FAAsync() :
                (await _socketsHttpHandler.CookieContainer.RememberCookieAndRemoveAsyncAfter(async ()
                    => await (await loginResponse.FollowRedirectWith(_noAutoRedirectHttpClient, cancellationToken)).FollowRedirectWith(_noAutoRedirectHttpClient, cancellationToken),
                    "_turbosquid_artist_session"))
                    .FollowRedirectWith(_httpClient, cancellationToken)))
                .EnsureSuccessStatusCode();


            async Task<HttpResponseMessage> SignInAsync() =>
                (await _noAutoRedirectHttpClient.PostAsync(
                    (this as IBaseAddressProvider).Endpoint("/users/sign_in?locale=en"),
                    credential._ToLoginMultipartFormData(),
                    cancellationToken))
                .EnsureRedirectStatusCode();

            async Task<HttpResponseMessage> SignInWith2FAAsync()
            {
                var _2faForm = await Get2FAFormAsync();
                var updatedCsrfToken = AuthenticityToken.ParseFromMetaTag(_2faForm);
                string verificationCode = (await _nodeGui.Request<string>(new InputRequest($"Please input the code for TurboSquid from your e-mail ({credential.UserName})"), cancellationToken)).Result;

                return await SignInWith2FAAsyncCore(verificationCode, credential.WithUpdated(updatedCsrfToken), cancellationToken);


                async Task<string> Get2FAFormAsync() => await
                    (await _httpClient.GetAsync(
                        (this as IBaseAddressProvider).Endpoint("/users/two_factor_authentication?locale=en"),
                        cancellationToken))
                    .EnsureSuccessStatusCode()
                    .Content.ReadAsStringAsync(cancellationToken);

                async Task<HttpResponseMessage> SignInWith2FAAsyncCore(string verificationCode, TurboSquidNetworkCredential credential, CancellationToken cancellationToken)
                {
                    var redirectingResponse = await Send2FAVerificationCodeFromEmailAsync(verificationCode);
                    redirectingResponse = (await redirectingResponse.FollowRedirectWith(_noAutoRedirectHttpClient, cancellationToken))
                        .EnsureRedirectStatusCode();

                    redirectingResponse = (await _socketsHttpHandler.CookieContainer.RememberCookieAndRemoveAsyncAfter(
                        async () => await redirectingResponse.FollowRedirectWith(_noAutoRedirectHttpClient, cancellationToken),
                        "_turbosquid_artist_session"))
                    .EnsureRedirectStatusCode();

                    return (await redirectingResponse.FollowRedirectWith(_httpClient, cancellationToken)).EnsureSuccessStatusCode();


                    async Task<HttpResponseMessage> Send2FAVerificationCodeFromEmailAsync(string verificationCode)
                        => (await _noAutoRedirectHttpClient.PostAsync(
                            (this as IBaseAddressProvider).Endpoint("/users/two_factor_authentication.user?locale=en"),
                            credential._To2FAFormUrlEncodedContentWith(verificationCode),
                            cancellationToken))
                        .EnsureRedirectStatusCode();
                }
            }
        }
    }

    #region Helpers

    bool _IsRedirectTo2FA(HttpResponseMessage response) =>
        response.Headers.Location!.AbsoluteUri.StartsWith((this as IBaseAddressProvider).Endpoint("/users/two_factor_authentication"));

    #endregion
}

static class RedirectHttpResponseMessageExtensions
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    internal static HttpResponseMessage EnsureRedirectStatusCode(this HttpResponseMessage response)
    {
        _logger.Debug($"{response.StatusCode} {response.Content}");
        if ((int) response.StatusCode < 300 || (int) response.StatusCode >= 400)
            throw new HttpRequestException("Response status code does not indicate redirect:", inner: null, response.StatusCode);
        else return response;
    }

    internal static async Task<HttpResponseMessage> FollowRedirectWith(this HttpResponseMessage response, HttpClient httpClient, CancellationToken cancellationToken)
        => await httpClient.GetAsync(response.Headers.Location!, cancellationToken);

    internal static HttpResponseMessage SetCookies(this HttpResponseMessage response, SocketsHttpHandler handler)
    {
        foreach (var cookie in response.Headers.SingleOrDefault(_ => _.Key == "Set-Cookie").Value
            .Select(value => value.Contains("Expires") ? value[..value.IndexOf("Expires")] + value[value.IndexOf("Expires")..].Replace('-', ' ').Replace("UTC", "GMT") : value)
            )
            handler.CookieContainer.SetCookies(response.RequestMessage!.RequestUri!, cookie);

        return response;
    }

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
    internal static async Task<HttpResponseMessage> RememberCookieAndRemoveAsyncAfter(
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
