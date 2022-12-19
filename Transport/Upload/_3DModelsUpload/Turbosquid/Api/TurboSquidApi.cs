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

            browser._DumpCookiesTo(_socketsHttpHandler.CookieContainer);

            credential_ = new(credential, csrfToken, applicationUserId, captchaVerifiedToken);
        });
        thread.Start();
        await Task.Run(thread.Join, cancellationToken);

        return credential_;
    }

    #endregion

    #region Login

    internal async Task _LoginAsyncUsing(TurboSquidNetworkCredential credential, CancellationToken cancellationToken) =>
        await _authenticationApi._LoginAsyncUsing(credential, cancellationToken);

    #endregion

    #region DraftCreation

    internal async Task<TurboSquid3DModelUploadSessionContext> _RequestModelUploadSessionContextAsyncFor(
        Composite3DModel composite3DModel,
        CancellationToken cancellationToken)
    {
        string csrfToken = await _RequestUploadInitializingCsrfTokenAsync(cancellationToken);
        string modelDraftId = await _CreateNewModelDraftAsync(cancellationToken);
        var awsUploadCredentials = await _RequestAwsUploadCredentialsAsync(csrfToken, cancellationToken);

        return new(new(composite3DModel, modelDraftId), awsUploadCredentials);
    }

    async Task<string> _RequestUploadInitializingCsrfTokenAsync(CancellationToken cancellationToken) =>
        CsrfToken._ParseFromMetaTag(
            await _httpClient.GetStringAsync(
                (this as IBaseAddressProvider).Endpoint("/turbosquid/products/new"),
                cancellationToken)
            );

    /// <returns>The ID of newly created model draft.</returns>
    async Task<string> _CreateNewModelDraftAsync(CancellationToken cancellationToken) =>
        (string)JObject.Parse(
            await _httpClient.GetStringAsync(
                (this as IBaseAddressProvider).Endpoint("/turbosquid/products/0/create_draft"),
                cancellationToken)
            )["id"]!;

    async Task<TurboSquidAwsUploadCredentials> _RequestAwsUploadCredentialsAsync(
        string csrfToken,
        CancellationToken cancellationToken) => await TurboSquidAwsUploadCredentials._AsyncFrom(
            await _httpClient.PostAsJsonAsync(
                (this as IBaseAddressProvider).Endpoint("/turbosquid/uploads//credentials"),
                new { authenticity_token = csrfToken },
                cancellationToken)
            );

    #endregion

    internal async Task _UploadAssetsAsync(TurboSquid3DModelUploadSessionContext uploadSessionContext, CancellationToken cancellationToken)
    {
        var uploadApi = new TurboSquidUploadApi(_httpClient, uploadSessionContext._Credentials);
        await uploadApi._UploadAssetsAsync(uploadSessionContext._Draft, cancellationToken);
    }
}
