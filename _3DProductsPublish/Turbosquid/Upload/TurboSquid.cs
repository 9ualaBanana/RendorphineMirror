using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Api;
using _3DProductsPublish.Turbosquid.Network;
using _3DProductsPublish.Turbosquid.Network.Authenticity;
using CefSharp.OffScreen;
using Microsoft.Net.Http.Headers;
using System.Net;

namespace _3DProductsPublish.Turbosquid.Upload;

internal partial class TurboSquid : HttpClient
{
    static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    internal static readonly Uri Origin = new("https://www.squid.io");

    internal required TurboSquidNetworkCredential Credential { get; init; }

    internal static async Task<TurboSquid> LogInAsyncUsing(NetworkCredential credential, CancellationToken cancellationToken)
    {
        var handler = new SocketsHttpHandler();
        var client = new TurboSquid(handler) { Credential = await RequestTurboSquidNetworkCredentialAsync() };
        await new TurboSquidAuthenticationApi(handler)._LoginAsyncUsing(client.Credential, cancellationToken);
        return client;

        async Task<TurboSquidNetworkCredential> RequestTurboSquidNetworkCredentialAsync()
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


            async Task<TurboSquidNetworkCredential> RequestTurboSquidNetworkCredentialAsyncCore()
            {
                TurboSquidNetworkCredential credential_ = null!;
                var thread = new Thread(() =>
                {
                    using var browser = new ChromiumWebBrowser(new Uri(Origin, "auth/keymaster").AbsoluteUri);
                    // Consider using ResourceRequestHandlerFactory.
                    browser.RequestHandler = new TurboSquidRequestHandler();
                    _ = browser.WaitForInitialLoadAsync().Result;

                    string credentialResponse = TurboSquidNetworkCredential._CapturedCefResponse.GetAsync(cancellationToken).Result;
                    string csrfToken = CsrfToken._ParseFromMetaTag(credentialResponse);
                    string applicationUserId = TurboSquidApplicationUserID._Parse(credentialResponse);
                    string captchaVerifiedTokenResponse = TurboSquidCaptchaVerifiedToken._CapturedCefResponse.GetAsync(cancellationToken).Result;
                    string captchaVerifiedToken = TurboSquidCaptchaVerifiedToken._Parse(captchaVerifiedTokenResponse);

                    browser._DumpCookiesTo(handler.CookieContainer);

                    credential_ = new(credential, csrfToken, applicationUserId, captchaVerifiedToken);
                });
                thread.Start();
                await Task.Run(thread.Join, cancellationToken);

                return credential_;
            }
        }
    }

    TurboSquid(SocketsHttpHandler handler) : base(handler)
    {
        BaseAddress = Origin;
        DefaultRequestHeaders.Add(HeaderNames.UserAgent, "gualabanana");
    }

    internal async Task PublishAsync(_3DProduct<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata> _3DProduct, CancellationToken cancellationToken)
    {
        try
        {
            _logger.Info($"Publishing {_3DProduct.Metadata.Title} 3D product contained inside {_3DProduct.ContainerPath}.");
            await
                (await
                    (await PublishSession.InitializeAsync(_3DProduct, this, cancellationToken))
                .StartAsync())
            .FinalizeAsync();
            _logger.Info($"{_3DProduct.Metadata.Title} 3D product has been published.");
        }
        catch (Exception ex)
        { _logger.Error(ex, $"{_3DProduct.Metadata.Title} 3D product publish failed."); }
    }
}