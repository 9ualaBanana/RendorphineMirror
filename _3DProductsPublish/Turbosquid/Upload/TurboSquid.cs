using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.CGTrader.Upload;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Api;
using _3DProductsPublish.Turbosquid.Network.Authenticity;
using Microsoft.Net.Http.Headers;
using System.Net;

namespace _3DProductsPublish.Turbosquid.Upload;

public partial class TurboSquid : HttpClient
{
    internal static readonly Logger _logger = LogManager.GetCurrentClassLogger();

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

    public async Task EditAsync(_3DProductDraft<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata> draft, CancellationToken cancellationToken)
    {
        await this.GetStringAndUpdateAuthenticityTokenAsync($"turbosquid/drafts/{draft.ID}/edit", cancellationToken);
        await PublishAsync(draft, cancellationToken);
    }

    public async Task PublishAsync(_3DProduct<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata> _3DProduct, CancellationToken cancellationToken)
        => await PublishAsync(await CreateDraftAsync(_3DProduct, cancellationToken), cancellationToken);
    async Task PublishAsync(_3DProductDraft<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata> draft, CancellationToken cancellationToken)
    {
        try
        {
            _logger.Info($"Publishing {draft.Product.Metadata.Title} 3D product contained inside {draft.Product.ContainerPath}.");
            await
                (await
                    (await PublishSession.InitializeAsync(draft, this, cancellationToken))
                .StartAsync())
            .FinalizeAsync();
            _logger.Info($"{draft.Product.Metadata.Title} 3D product has been published.");
        }
        catch (Exception ex)
        { _logger.Error(ex, $"{draft.Product.Metadata.Title} 3D product publish failed."); }
    }

    // Body part under <script> tag in the responses contains all the necessary information/references related to the product and its manipulation.
    // Refactor the code to use those.
    async Task<_3DProductDraft<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata>> CreateDraftAsync(
        _3DProduct<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata> _3DProduct,
        CancellationToken cancellationToken)
    {
        try
        {
            var draft = new _3DProductDraft<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata>(_3DProduct, _3DProduct.ID is 0 ? await NewAsync() : await EditAsync());
            _logger.Trace($"3D product draft with {draft.ID} ID has been created for {_3DProduct.Metadata.Title}.");
            return draft;


            // Performs default initialization of gon.product.
            async Task<string> NewAsync()
            { await this.GetStringAndUpdateAuthenticityTokenAsync("turbosquid/products/new", cancellationToken); return await CreateDraftAsync(); }

            async Task<string> EditAsync()
                => (await this.GetStringAndUpdateAuthenticityTokenAsync($"turbosquid/products/{_3DProduct.ID}/edit", cancellationToken)).JsonValue("draft_id") ?? await CreateDraftAsync();

            async Task<string> CreateDraftAsync()
                => JObject.Parse(await GetStringAsync($"turbosquid/products/{_3DProduct.ID}/create_draft", cancellationToken))["id"]!.Value<string>()!;
        }
        catch (Exception ex)
        { throw new Exception($"Failed to create a 3D product draft for {_3DProduct.Metadata.Title}.", ex); }
    }
}
