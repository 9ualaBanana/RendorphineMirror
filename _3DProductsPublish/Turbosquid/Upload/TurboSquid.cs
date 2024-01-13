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

    public required TurboSquidNetworkCredential Credential { get; init; }

    public static async Task<TurboSquid> LogInAsyncUsing(NetworkCredential credential, INodeGui nodeGui, CancellationToken cancellationToken)
    {
        var handler = new SocketsHttpHandler();
        var authenticationApi = new TurboSquidAuthenticationApi(handler, nodeGui);
        var client = new TurboSquid(handler) { Credential = await authenticationApi.RequestTurboSquidNetworkCredentialAsync(credential, cancellationToken) };
        await authenticationApi._LoginAsyncUsing(client.Credential, cancellationToken);
        return client;
    }

    TurboSquid(SocketsHttpHandler handler) : base(handler)
    {
        BaseAddress = Origin;
        DefaultRequestHeaders.Add(HeaderNames.UserAgent, "gualabanana");
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
        string host = "www.turbosquid.com";
        int salesReportsId = 20;
        int year = 2024;
        int month = 1;
        var path = $"/Report/Index.cfm";
        var query = $"report_id={salesReportsId}&xsl=1&theyear={year}&theMonth={month}";
        var uri = new UriBuilder
        {
            Scheme = "https",
            Host = host,
            Path = path,
            Query = query
        }.Uri;
        var report = (await GetAsync(uri, cancellationToken)).EnsureSuccessStatusCode();
    }
}
