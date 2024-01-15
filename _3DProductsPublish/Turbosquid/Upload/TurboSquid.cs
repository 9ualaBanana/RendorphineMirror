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
    readonly SocketsHttpHandler _handler;
    readonly HttpClient _noAutoRedirectHttpClient;

    public required TurboSquidNetworkCredential Credential { get; init; }

    public static async Task<TurboSquid> LogInAsyncUsing(NetworkCredential credential, INodeGui nodeGui, CancellationToken cancellationToken)
    {
        var handler = new SocketsHttpHandler();
        var authenticationApi = new TurboSquidAuthenticationApi(handler, nodeGui);
        var client = new TurboSquid(handler) { Credential = await authenticationApi.RequestTurboSquidNetworkCredentialAsync(credential, cancellationToken) };
        await authenticationApi._LoginAsyncUsing(client.Credential, cancellationToken);
        // client_uid header gets duplicated for two different domains: www.squid.io and auth.turbosquid.com.
        return client;
    }

    TurboSquid(SocketsHttpHandler handler) : base(handler)
    {
        _handler = handler;
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

    public async Task ParseSalesAsync(CancellationToken cancellationToken)
    {
        int month = 1; int year = 2024;
        var report = (await _noAutoRedirectHttpClient.GetAsync(SaleReport.Uri.For(month, year), cancellationToken)).SetCookies(_handler);
        report = (await report._FollowRedirectWith(_noAutoRedirectHttpClient, cancellationToken)).SetCookies(_handler);    // Redirects to https://www.turbosquid.com/Login/Index.cfm?stgRU=https%3A%2F%2Fwww.turbosquid.com%2FReport%2FIndex.cfm%3Freport_id%3D20
        // Contains only _keymaster_session cookie which doesn't have Expires property.
        report = (await report._FollowRedirectWith(_noAutoRedirectHttpClient, cancellationToken)).SetCookies(_handler);    // Redirects to https://auth.turbosquid.com/oauth/authorize?client_id=2c781a9f16cbd4fded77cf7f47db1927b85a5463185769bcb970cfdfe7463a0c&state=eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE3MDUyNDY2NjguNSwic3ViIjoiUmVwb3J0L0luZGV4LmNmbSIsImV4cCI6MTcwNTI0NjY5OC41LCJqdGkiOiJDRDFCOUEwNi05RDcyLTQxNEUtOUI5NzNBMDVCMkMzMTY5RCJ9.3xeC5SohQT665dLD53L-UWAbT-zR_5a6ETGzzL_B9Aw&response_type=code&redirect_uri=https://www.turbosquid.com/Login/Keymaster.cfm?endpoint=callback&scope=id%20email%20roles%20device
        report = (await report._FollowRedirectWith(_noAutoRedirectHttpClient, cancellationToken)).SetCookies(_handler);    // Redirects to https://www.turbosquid.com/Login/Keymaster.cfm?endpoint=callback&code=e5e6d7dadca042c277a24a5688de356ea4e1c6aef298723ce5201803e537445c&state=eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE3MDUyNDY2NjguNSwic3ViIjoiUmVwb3J0L0luZGV4LmNmbSIsImV4cCI6MTcwNTI0NjY5OC41LCJqdGkiOiJDRDFCOUEwNi05RDcyLTQxNEUtOUI5NzNBMDVCMkMzMTY5RCJ9.3xeC5SohQT665dLD53L-UWAbT-zR_5a6ETGzzL_B9Aw
        report = await report._FollowRedirectWith(this, cancellationToken); // Final redirect to https://www.turbosquid.com/Report/Index.cfm?report_id=20
    }
}
