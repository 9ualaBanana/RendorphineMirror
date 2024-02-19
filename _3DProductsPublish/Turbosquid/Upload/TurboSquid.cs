﻿using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.CGTrader.Upload;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Api;
using _3DProductsPublish.Turbosquid.Network.Authenticity;
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

    public async Task PublishAsync(TurboSquid3DProduct _3DProduct, CancellationToken cancellationToken)
        => await PublishAsync(await CreateDraftAsync(_3DProduct, cancellationToken), cancellationToken);
    async Task PublishAsync(TurboSquid3DProductDraft draft, CancellationToken cancellationToken)
    {
        try
        {
            _logger.Info($"Publishing {draft.LocalProduct.Metadata.Title} 3D product contained inside {draft.LocalProduct.ContainerPath}.");
            await
                (await
                    (await PublishSession.InitializeAsync(draft, this, cancellationToken))
                .StartAsync())
            .FinalizeAsync();
            _logger.Info($"{draft.LocalProduct.Metadata.Title} 3D product has been published.");
        }
        catch (Exception ex)
        { _logger.Error(ex, $"{draft.LocalProduct.Metadata.Title} 3D product publish failed."); }
    }

    // Body part under <script> tag in the responses contains all the necessary information/references related to the product and *its manipulation*.
    // Refactor the code to use those.
    async Task<TurboSquid3DProductDraft> CreateDraftAsync(TurboSquid3DProduct _3DProduct, CancellationToken cancellationToken)
    {
        try
        {
            var remote = TurboSquid3DProductMetadata.Product.Parse(_3DProduct.ID is 0 ? await NewAsync() : await EditAsync());

            // Currently all created drafts are published, so if there is a draft already, it will be deleted not to mess up synchronization.
            //if (remote.draft_id is not (null or 0))
            //{ await DeleteDraftAsync(); remote = TurboSquid3DProductMetadata.Product.Parse(await EditAsync()); }

            var draft = new TurboSquid3DProductDraft(await CreateDraftAsync(), await RequestAwsStorageCredentialAsync(), _3DProduct, remote);
            _logger.Trace($"3D product draft with {draft.ID} ID has been created for {_3DProduct.Metadata.Title}.");
            return draft;


            async Task<string> NewAsync()
                => await this.GetStringAndUpdateAuthenticityTokenAsync("turbosquid/products/new", cancellationToken);

            async Task<string> EditAsync()
                => await this.GetStringAndUpdateAuthenticityTokenAsync($"turbosquid/products/{_3DProduct.ID}/edit", cancellationToken);

            async Task<long> CreateDraftAsync()
                => JObject.Parse(await GetStringAsync($"turbosquid/products/{_3DProduct.ID}/create_draft", cancellationToken))["id"]!.Value<long>()!;

            // TODO: Response returns 422 and the product doesn't get deleted which preserves draft information in `remote`. `/create_draft` returns already existing draft ID in such case.
            async Task DeleteDraftAsync()
                => await DeleteAsync($"turbosquid/products/{_3DProduct.ID}/delete_draft", cancellationToken);

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
}
