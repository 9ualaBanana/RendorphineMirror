using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid.Api;
using _3DProductsPublish.Turbosquid.Network.Authenticity;
using MarkTM.RFProduct;
using Microsoft.Net.Http.Headers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;

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

    readonly ConcurrentDictionary<string, ManualResetEventSlim> _productsLocker = [];
    public async Task UploadAsync(RFProduct rfProduct, INodeGui gui, CancellationToken cancellationToken)
    {
        var productUploadEvent = _productsLocker.GetOrAdd(rfProduct.Name, new ManualResetEventSlim(true));
        try
        {
            var idea = (RFProduct._3D.Idea_)rfProduct.Idea;
            if (!productUploadEvent.Wait(TimeSpan.FromMinutes(10), cancellationToken))
                throw new OperationCanceledException("Timeout waiting for product upload is expired.");
            productUploadEvent.Reset();   // Acquire lock before metadata is read to detect its modifications.
            var rfProduct3D = await rfProduct.ToTurboSquid3DProductAsync(cancellationToken);

            if (rfProduct3D.Metadata.Status is RFProduct._3D.Status.none)
            { _logger.Warn($"{rfProduct3D.Metadata.Title} 3D {nameof(RFProduct)} is not uploaded due to its status being set to {RFProduct._3D.Status.none}."); return; }

            await UploadAsync(rfProduct3D, cancellationToken);

            idea.Status = (await CreateOrRequestRemoteAsync(rfProduct3D, cancellationToken)).status;
        }
        finally { productUploadEvent.Set(); }
    }
    public async Task UploadAsync(_3DProduct _3DProduct, CancellationToken cancellationToken)
    {
        try
        {
            _logger.Info($"Publishing {_3DProduct.Metadata.Title} 3D product contained inside {_3DProduct.ContainerPath}.");
            await
                (await
                    (await PublishSession.InitializeAsync(await CreateDraftAsync(_3DProduct, cancellationToken), this, cancellationToken))
                .StartAsync())
            .FinalizeAsync();
            _logger.Info($"{_3DProduct.Metadata.Title} 3D product has been published.");
        }
        catch (Exception ex)
        { _logger.Error(ex, $"{_3DProduct.Metadata.Title} 3D product publish failed."); }
    }

    // Body part under <script> tag in the responses contains all the necessary information/references related to the product and *its manipulation*.
    // Refactor the code to use those.
    /// <remarks>
    /// Prioritazes <see cref="_3DProduct.DraftID"/> over <see cref="_3DProduct.ID"/>.
    /// </remarks>
    async Task<_3DProduct.Draft> CreateDraftAsync(_3DProduct _3DProduct, CancellationToken cancellationToken)
    {
        try
        {
            _3DProduct.Remote remote = await CreateOrRequestRemoteAsync(_3DProduct, cancellationToken);
            if ((_3DProduct.DraftID = remote.draft_id ?? 0) is 0)
                _3DProduct.DraftID = await CreateDraftAsync();
            var draft = new _3DProduct.Draft(await RequestAwsStorageCredentialAsync(), _3DProduct, remote);
            _logger.Trace($"3D product draft with {draft.LocalProduct.DraftID} ID has been created for {_3DProduct.Metadata.Title}.");
            return draft;


            // Returns the ID of the newly created or already existing draft for the given `_3DProduct.ID`.
            async Task<long> CreateDraftAsync()
                => JObject.Parse(await GetStringAsync($"turbosquid/products/{_3DProduct.ID}/create_draft", cancellationToken))["id"]!.Value<long>()!;

            async Task<TurboSquidAwsSession> RequestAwsStorageCredentialAsync()
            {
                var authenticity_token = Credential.AuthenticityToken;
                try
                {
                    var awsCredential = TurboSquidAwsSession.Parse(await
                        (await this.PostAsJsonAsync("turbosquid/uploads//credentials", new { authenticity_token }, cancellationToken))
                        .EnsureSuccessStatusCode()
                        .Content.ReadAsStringAsync(cancellationToken));
                    _logger.Trace($"AWS credential for {authenticity_token} session has been obtained.");
                    return awsCredential;
                }
                catch (Exception ex)
                { throw new Exception($"AWS credential request for {authenticity_token} session failed.", ex); }
            }
        }
        catch (Exception ex)
        { throw new Exception($"Failed to create a 3D product draft for {_3DProduct.Metadata.Title}.", ex); }
    }


    async Task<_3DProduct.Remote> CreateOrRequestRemoteAsync(_3DProduct _3DProduct, CancellationToken cancellationToken)
    {
        var response = await (_3DProduct.ID is 0 && _3DProduct.DraftID is 0 ? NewAsync() : EditAsync());
        _logger.Debug($"Remote product response:\n{response}");
        return _3DProduct.Remote.Parse(response);


        async Task<string> NewAsync()
            => await this.GetStringAndUpdateAuthenticityTokenAsync("turbosquid/products/new", cancellationToken);

        async Task<string> EditAsync()
        {
            bool isDraft = _3DProduct.DraftID is not 0 && _3DProduct.ID is 0;
            var id = isDraft ? _3DProduct.DraftID : _3DProduct.ID;

            return await this.GetStringAndUpdateAuthenticityTokenAsync($"turbosquid/{(isDraft ? "drafts" : "products")}/{id}/edit", cancellationToken);
        }
    }

    // Deletes the draft for the given `_3DProduct.ID`.
    async Task DeleteDraftAsync(_3DProduct _3DProduct, CancellationToken cancellationToken)
        => await SendAsync(new(HttpMethod.Delete, $"turbosquid/products/{_3DProduct.ID}/delete_draft")
        { Content = JsonContent.Create(new { authenticity_token = Credential.AuthenticityToken }) },
        cancellationToken);
}

static class TurboSquid3DProductExtensions
{
    internal static async Task<TurboSquid._3DProduct> ToTurboSquid3DProductAsync(this RFProduct rfProduct, CancellationToken cancellationToken)
    {
        var idea = (RFProduct._3D.Idea_)rfProduct.Idea;
        var metadata = JObject.Parse(File.ReadAllText(idea.Metadata)).ToObject<_3DProduct.Metadata_>()!;
        return await new _3DProduct(idea.Path,
            idea.Packages.Select(_ => new _3DModel(_)).ToList(),
            idea.Renders.Select(_ => new _3DProductThumbnail(_)).ToList(),
            [])
            .AsyncWithTurboSquid(metadata, cancellationToken);
        // Decouple that shit like metadata and bare _3DProduct.
        // _3DProduct is just a fucking data structure representing a 3D product on the file system.
    }
}
