using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.CGTrader.Upload;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Network;
using _3DProductsPublish.Turbosquid.Network.Authenticity;
using _3DProductsPublish.Turbosquid.Upload;
using CefSharp.OffScreen;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Net.Http.Json;

namespace _3DProductsPublish.Turbosquid.Api;

internal class TurboSquidApi
{
    readonly static ILogger _logger = LogManager.GetCurrentClassLogger();

    readonly SocketsHttpHandler _socketsHttpHandler;
    readonly HttpClient _httpClient;
    readonly TurboSquidAuthenticationApi _authenticationApi;

    internal static readonly Uri _BaseUri = new("https://www.squid.io");

    internal TurboSquidApi()
    {
        _socketsHttpHandler = new();
        _httpClient = new(_socketsHttpHandler);
        _authenticationApi = new(_socketsHttpHandler);

        _httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "gualabanana");
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


        async Task<TurboSquidNetworkCredential> _RequestTurboSquidNetworkCredentialAsyncCore(NetworkCredential credential, CancellationToken cancellationToken)
        {
            TurboSquidNetworkCredential credential_ = null!;
            var thread = new Thread(() =>
            {
                using var browser = new ChromiumWebBrowser(new Uri(_BaseUri, "auth/keymaster").AbsoluteUri);
                // Consider using ResourceRequestHandlerFactory.
                browser.RequestHandler = new TurboSquidRequestHandler();
                _ = browser.WaitForInitialLoadAsync().Result;

                string credentialResponse = TurboSquidNetworkCredential._CapturedCefResponse.GetAsync(cancellationToken).Result;
                string csrfToken = CsrfToken._ParseFromMetaTag(credentialResponse);
                string applicationUserId = TurboSquidApplicationUserID._Parse(credentialResponse);
                string captchaVerifiedTokenResponse = TurboSquidCaptchaVerifiedToken._CapturedCefResponse.GetAsync(cancellationToken).Result;
                string captchaVerifiedToken = TurboSquidCaptchaVerifiedToken._Parse(captchaVerifiedTokenResponse);

                browser._DumpCookiesTo(_socketsHttpHandler.CookieContainer);

                credential_ = new(credential, csrfToken, applicationUserId, captchaVerifiedToken);
            });
            thread.Start();
            await Task.Run(thread.Join, cancellationToken);

            return credential_;
        }
    }

    #endregion

    #region Login

    internal async Task _LoginAsyncUsing(TurboSquidNetworkCredential credential, CancellationToken cancellationToken) =>
        await _authenticationApi._LoginAsyncUsing(credential, cancellationToken);

    #endregion

    #region DraftCreation

    internal async Task<TurboSquid3DProductUploadSessionContext> RequestProductUploadSessionContextAsyncFor(
        _3DProduct _3DProduct,
        TurboSquidNetworkCredential credential,
        CancellationToken cancellationToken)
    {
        var metadata = TurboSquid3DProductMetadata.For(_3DProduct);
        string csrfToken = await RequestUploadInitializingAuthenticityTokenAsync(cancellationToken);
        string productDraftId = await CreateNewProductDraftAsync(cancellationToken);
        var awsUploadCredentials = await RequestAwsUploadCredentialsAsync(csrfToken, cancellationToken);

        return new TurboSquid3DProductUploadSessionContext(
            new _3DProductDraft(_3DProduct, productDraftId),
            metadata,
            credential.WithUpdated(csrfToken),
            awsUploadCredentials);


        /// <returns>CSRF token that must be passed to each request that is part of uploading process that follows.</returns>
        async Task<string> RequestUploadInitializingAuthenticityTokenAsync(CancellationToken cancellationToken) =>
            CsrfToken._ParseFromMetaTag(
                await _httpClient.GetStringAsync(
                    new Uri(_BaseUri, "turbosquid/products/new"),
                    cancellationToken)
                );

        /// <returns>The ID of newly created model draft.</returns>
        async Task<string> CreateNewProductDraftAsync(CancellationToken cancellationToken) =>
            (string)JObject.Parse(
                await _httpClient.GetStringAsync(
                    new Uri(_BaseUri, "turbosquid/products/0/create_draft"),
                    cancellationToken)
                )["id"]!;

        async Task<TurboSquidAwsUploadCredentials> RequestAwsUploadCredentialsAsync(
            string csrfToken,
            CancellationToken cancellationToken)
        {
            var response = await
                (await _httpClient.PostAsJsonAsync(
                    new Uri(_BaseUri, "turbosquid/uploads//credentials"),
                    new { authenticity_token = csrfToken },
                    cancellationToken)
                )
                .EnsureSuccessStatusCode()
                .Content.ReadAsStringAsync(cancellationToken);
            
            return TurboSquidAwsUploadCredentials.Parse(response);
        }
    }

    #endregion

    internal async Task PublishAsyncUsing(
        TurboSquid3DProductUploadSessionContext productUploadSessionContext,
        CancellationToken cancellationToken)
    {
        var uploadApi = new TurboSquidUploadApi(_httpClient, productUploadSessionContext);
        var uploadedAssets = await uploadApi.UploadAssetsAsync(cancellationToken);
        var publishApi = new TurboSquidPublishApi(_httpClient, productUploadSessionContext, uploadedAssets);
        await publishApi.UploadMetadataAsync(cancellationToken);
        await publishApi.PublishProductAsync(cancellationToken);
    }
}
