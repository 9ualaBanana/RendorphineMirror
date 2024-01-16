using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Api;
using _3DProductsPublish.Turbosquid.Network.Authenticity;
using Microsoft.Net.Http.Headers;
using System.Net;

namespace _3DProductsPublish.Turbosquid.Upload;

public partial class TurboSquid : HttpClient
{
    static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    internal static readonly Uri Origin = new("https://www.squid.io");
    internal readonly SocketsHttpHandler Handler;
    internal readonly HttpClient _noAutoRedirectHttpClient;

    public required TurboSquidNetworkCredential Credential { get; init; }

    // TODO: Implement lazy authentication.
    public SaleReports_ SaleReports { get; private set; } = null!;

    public static async Task<TurboSquid> LogInAsyncUsing(NetworkCredential credential, INodeGui nodeGui, CancellationToken cancellationToken)
    {
        var handler = new SocketsHttpHandler();
        var authenticationApi = new TurboSquidAuthenticationApi(handler, nodeGui);
        var client = new TurboSquid(handler) { Credential = await authenticationApi.RequestTurboSquidNetworkCredentialAsync(credential, cancellationToken) };
        await authenticationApi._LoginAsyncUsing(client.Credential, cancellationToken);
        // client_uid header gets duplicated for two different domains: www.squid.io and auth.turbosquid.com.

        client.SaleReports = await SaleReports_.LoginAsync(client, cancellationToken);

        return client;
    }

    TurboSquid(SocketsHttpHandler handler) : base(handler)
    {
        Handler = handler;
        BaseAddress = Origin;
        DefaultRequestHeaders.Add(HeaderNames.UserAgent, "gualabanana");
        _noAutoRedirectHttpClient = new HttpClient(new SocketsHttpHandler()
        {
            AllowAutoRedirect = false,
            CookieContainer = handler.CookieContainer
        });
        _noAutoRedirectHttpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "gualabanana");
    }

    public async Task PublishAsync(_3DProduct<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata> _3DProduct, CancellationToken cancellationToken)
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
