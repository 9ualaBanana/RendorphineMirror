using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid.Api;
using _3DProductsPublish.Turbosquid.Network.Authenticity;
using MarkTM.RFProduct;
using Microsoft.Net.Http.Headers;
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

    public async Task PublishAsync(RFProduct rfProduct, INodeGui gui, CancellationToken cancellationToken)
    {
        var idea = (RFProduct._3D.Idea_)rfProduct.Idea;
        var rfProduct3D = await ConvertAsync(rfProduct, gui, cancellationToken);

        if (rfProduct3D.Metadata.Status is RFProduct._3D.Status.none)
        { _logger.Warn($"{rfProduct3D.Metadata.Title} 3D {nameof(RFProduct)} is not published due to its status being set to {RFProduct._3D.Status.none}."); return; }

        // `_3DProduct.ID` here represents either the ID of the product if `_Status.json` is `online` or the ID of the draft if `_Status.json` is `draft`
        // due to poorly implemented serialization.
        if (idea.Status is RFProduct._3D.Status.draft)
        { rfProduct3D.DraftID = rfProduct3D.ID; rfProduct3D.ID = default; }

        await PublishAsync(rfProduct3D, cancellationToken);

        //idea.Status = rfProduct3D.Metadata.Status;
        // чтоб нахуй не дергал, похуй вообще
        idea.Status = _3DProduct.Remote.Parse(await EditAsync(rfProduct3D, cancellationToken)).status;


        static async Task<_3DProduct> ConvertAsync(RFProduct rfProduct, INodeGui gui, CancellationToken cancellationToken)
        {
            var idea = (RFProduct._3D.Idea_)rfProduct.Idea;
            var metadata = JObject.Parse(File.ReadAllText(idea.Metadata)).ToObject<_3DProductDS._3DProduct.Metadata_>()!;
            return await _3DProductDS._3DProduct.FromDirectory(idea.Path)
                .AsyncWithTurboSquid(metadata, cancellationToken);// Decouple that shit like metadata and bare _3DProduct.
            // _3DProduct is just a fucking data structure representing a 3D product on the file system.
        }
    }
    public async Task PublishAsync(_3DProduct _3DProduct, CancellationToken cancellationToken)
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
    async Task<_3DProduct.Draft> CreateDraftAsync(_3DProduct _3DProduct, CancellationToken cancellationToken)
    {
        try
        {
            _3DProduct.Draft draft;
            if (_3DProduct.DraftID is not 0 || _3DProduct.ID is not 0)
            {
                var remote = _3DProduct.Remote.Parse(await EditAsync(_3DProduct, cancellationToken));
                draft = new _3DProduct.Draft(await RequestAwsStorageCredentialAsync(), _3DProduct, remote);
            }
            else
            {
                var remote = _3DProduct.Remote.Parse(await NewAsync());
                draft = new _3DProduct.Draft(await RequestAwsStorageCredentialAsync(), _3DProduct with { DraftID = await CreateDraftAsync() }, remote);


                async Task<string> NewAsync()
                    => await this.GetStringAndUpdateAuthenticityTokenAsync("turbosquid/products/new", cancellationToken);
            }

            // If there is a draft already, it will be deleted not to mess up synchronization.
            //if (remote.draft_id is not (null or 0))
            //{ await DeleteDraftAsync(); remote = TurboSquid3DProductMetadata.Product.Parse(await EditAsync()); }

            _logger.Trace($"3D product draft with {draft.LocalProduct.DraftID} ID has been created for {_3DProduct.Metadata.Title}.");
            return draft;


            // Returns the ID of the newly created or already existing draft for the given `_3DProduct.ID`.
            async Task<long> CreateDraftAsync()
                => JObject.Parse(await GetStringAsync($"turbosquid/products/{_3DProduct.ID}/create_draft", cancellationToken))["id"]!.Value<long>()!;

            // Deletes the draft for the given `_3DProduct.ID`.
            async Task DeleteDraftAsync()
                => await SendAsync(new(HttpMethod.Delete, $"turbosquid/products/{_3DProduct.ID}/delete_draft")
                { Content = JsonContent.Create(new { authenticity_token = Credential.AuthenticityToken }) },
                cancellationToken);

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

    async Task<string> EditAsync(_3DProduct _3DProduct, CancellationToken cancellationToken)
    {
        bool isDraft = _3DProduct.DraftID is not 0 && _3DProduct.ID is 0;
        var id = isDraft ? _3DProduct.DraftID : _3DProduct.ID;

        return await this.GetStringAndUpdateAuthenticityTokenAsync($"turbosquid/{(isDraft ? "drafts" : "products")}/{id}/edit", cancellationToken);
    }
}
