using CefSharp;
using CefSharp.OffScreen;
using Newtonsoft.Json.Linq;
using NLog;
using System.Net;
using System.Net.Http.Json;
using Transport.Models;
using Transport.Upload._3DModelsUpload.Turbosquid.Network;
using Transport.Upload._3DModelsUpload.Turbosquid.Network.Authenticity;
using Transport.Upload._3DModelsUpload.Turbosquid.Upload;

namespace Transport.Upload._3DModelsUpload.Turbosquid.Api;

internal class TurboSquidApi : IBaseAddressProvider
{
    readonly static ILogger _logger = LogManager.GetCurrentClassLogger();

    readonly SocketsHttpHandler _socketsHttpHandler;
    readonly HttpClient _httpClient;
    readonly TurboSquidAuthenticationApi _authenticationApi;

    string IBaseAddressProvider.BaseAddress => "https://www.squid.io/";

    internal TurboSquidApi()
    {
        // CookieContainer simply doesn't store any cookies set by any response, but sometimes it does. That's some stupid shit.
        _socketsHttpHandler = new();
        _httpClient = new(_socketsHttpHandler);
        _authenticationApi = new(_socketsHttpHandler);
    }

    #region Credential

    internal async Task<TurboSquidNetworkCredential> _RequestTurboSquidNetworkCredentialAsync(NetworkCredential credential, CancellationToken cancellationToken)
    {
        try
        {
            var credential_ = await _RequestTurboSquidNetworkCredentialAsyncCore(credential, cancellationToken);
            _logger.Debug("{Credential} was received.", nameof(TurboSquidNetworkCredential));
            return credential_;
        }
        catch (Exception ex)
        {
            string errorMessage = $"{nameof(TurboSquidNetworkCredential)} request failed.";
            _logger.Error(ex, errorMessage); throw new Exception(errorMessage, ex);
        }
    }

    async Task<TurboSquidNetworkCredential> _RequestTurboSquidNetworkCredentialAsyncCore(NetworkCredential credential, CancellationToken cancellationToken)
    {
        TurboSquidNetworkCredential credential_ = null!;
        var thread = new Thread(() =>
        {
            using var browser = new ChromiumWebBrowser((this as IBaseAddressProvider).Endpoint("/auth/keymaster"));
            // Consider using ResourceRequestHandlerFactory.
            browser.RequestHandler = new TurboSquidRequestHandler();

            string credentialResponse = TurboSquidNetworkCredential._ServerResponse.GetAsync(cancellationToken).Result;
            string csrfToken = CsrfToken._ParseFromMetaTag(credentialResponse);
            string applicationUserId = TurboSquidApplicationUserID._Parse(credentialResponse);
            string captchaVerifiedTokenResponse = TurboSquidCaptchaVerifiedToken._ServerResponse.GetAsync(cancellationToken).Result;
            string captchaVerifiedToken = TurboSquidCaptchaVerifiedToken._Parse(captchaVerifiedTokenResponse);

            // Cookie dumping takes place after the last redirect to https://auth.turbosquid.com/users/sign_in, i.e. clients are switched from CEF to HttpClient.
            // e.g. Response from https://auth.turbosquid.com/users/sign_in?locale=en sets `_keymaster_session` cookie but it adds the new one instead of updating the same existing one
            // (only seen with Fiddler, CookieContainer doesn't contain duplicates). The same goes for `_turbosquid_artist_session`.
            // After that this cookie stops being updated and the new one is added and this is the one being updated from now on.
            browser.GetCookieManager().VisitAllCookies(new CookieCopyingVisitor(_socketsHttpHandler.CookieContainer));

            credential_ = new(credential, csrfToken, applicationUserId, captchaVerifiedToken);
        });
        thread.Start();
        await Task.Run(thread.Join, cancellationToken);

        return credential_;
    }

    #endregion

    #region Login

    internal async Task _LoginAsync(TurboSquidNetworkCredential credential, CancellationToken cancellationToken) =>
        await _authenticationApi._LoginAsync(credential, cancellationToken);

    #endregion

    #region DraftCreation

    internal async Task<TurboSquid3DModelUploadSessionContext> _RequestModelUploadSessionDataAsyncFor(
        Composite3DModel composite3DModel,
        CancellationToken cancellationToken)
    {
        string csrfToken = await _RequestUploadInitializingCsrfTokenAsync(cancellationToken);
        string modelDraftId = await _CreateNewModelDraftAsync(cancellationToken);
        var modelUploadCredentials = await _RequestModelUploadCredentialsAsync(csrfToken, cancellationToken);

        return new(new(composite3DModel, modelDraftId), modelUploadCredentials);
    }

    async Task<string> _RequestUploadInitializingCsrfTokenAsync(CancellationToken cancellationToken) =>
        CsrfToken._ParseFromMetaTag(
            await _httpClient.GetStringAsync(
                (this as IBaseAddressProvider).Endpoint("/turbosquid/products/new"),
                cancellationToken)
            );

    async Task<string> _CreateNewModelDraftAsync(CancellationToken cancellationToken) =>
        (string)JObject.Parse(
            await _httpClient.GetStringAsync(
                (this as IBaseAddressProvider).Endpoint("/turbosquid/products/0/create_draft"),
                cancellationToken)
            )["id"]!;

    async Task<TurboSquid3DModelUploadCredentials> _RequestModelUploadCredentialsAsync(
        string csrfToken,
        CancellationToken cancellationToken) => await TurboSquid3DModelUploadCredentials._AsyncFrom(
            await _httpClient.PostAsJsonAsync(
                (this as IBaseAddressProvider).Endpoint("/turbosquid/uploads//credentials"),
                new { authenticity_token = csrfToken },
                cancellationToken)
            );

    #endregion
}

static class TurboSquidApiExtensions
{
    internal static HttpRequestMessage _WithHostHeader(this HttpRequestMessage request)
    { request.Headers.Host = "www.squid.io"; return request; }
}
