using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid.Api;
using _3DProductsPublish.Turbosquid.Network.Authenticity;
using MarkTM.RFProduct;
using Microsoft.Net.Http.Headers;
using System.Collections.Concurrent;
using System.Net;
using static _3DProductsPublish._3DProductDS._3DProduct.Metadata_;

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
            if (!productUploadEvent.Wait(TimeSpan.FromMinutes(10), cancellationToken))
                throw new OperationCanceledException("Timeout waiting for product upload is expired.");
            productUploadEvent.Reset();   // Acquire lock before metadata is read to detect its modifications.
            var rfProduct3D = await rfProduct.ToTurboSquid3DProductAsync(this, cancellationToken);

            if (rfProduct3D.Metadata.Status is RFProduct._3D.Status.none)
            { _logger.Warn($"{rfProduct3D.Metadata.Title} 3D {nameof(RFProduct)} is not uploaded due to its status being set to {RFProduct._3D.Status.none}."); return; }

            var session = await PublishSession.InitializeAsync(rfProduct3D, this, cancellationToken);
            await UploadAsyncCore();

            var sumbitjsonpath = ((RFProduct._3D.Idea_)rfProduct.Idea).Metadata;
            var submitjson = JObject.Parse(await File.ReadAllTextAsync(sumbitjsonpath, cancellationToken));
            var metajson = JObject.Parse(JsonConvert.SerializeObject(session._3DProduct.Tracker.Data, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented }));
            metajson["TSCategory"] = metajson["Category"]; metajson.Remove("Category");
            metajson["TSSubCategory"] = metajson["SubCategory"]; metajson.Remove("SubCategory");
            submitjson.Merge(metajson);
            await File.WriteAllTextAsync(sumbitjsonpath, JsonConvert.SerializeObject(submitjson, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented }), cancellationToken);


            async Task UploadAsyncCore()
            {
                try
                {
                    _logger.Info($"Uploading {rfProduct3D.Metadata.Title} 3D product stored inside {rfProduct3D.ContainerPath}.");
                    await (await session.StartAsync()).FinalizeAsync();
                    _logger.Info($"{rfProduct3D.Metadata.Title} 3D product has been published.");
                }
                catch (Exception ex)
                { _logger.Error(ex, $"{rfProduct3D.Metadata.Title} 3D product upload failed."); }
            }
        }
        finally { productUploadEvent.Set(); }
    }

    internal async Task<_3DProduct.Remote> CreateOrRequestRemoteAsync(_3DProduct _3DProduct, CancellationToken cancellationToken)
    {
        do
        {
            var response = await (_3DProduct.Tracker.Data.ProductID is 0 && _3DProduct.Tracker.Data.DraftID is 0 ? NewAsync() : EditAsync());
            var remote = _3DProduct.Remote.Parse(response);
            if (remote.status is RFProduct._3D.Status.deleted)
            { _3DProduct.Tracker.Reset(); _3DProduct.Tracker.Write(); }
            else
                return remote;
        }
        while (true);


        async Task<string> NewAsync()
        {
            try
            {
                var response = await this.GetStringAndUpdateAuthenticityTokenAsync("turbosquid/products/new", cancellationToken);
                _logger.Trace("Remote product has been initialized.");
                return response;
            }
            catch { _logger.Error("Remote product initialization failed."); throw; }
        }

        async Task<string> EditAsync()
        {
            bool isDraft = _3DProduct.Tracker.Data.DraftID is not 0 && _3DProduct.Tracker.Data.ProductID is 0;
            var id = isDraft ? _3DProduct.Tracker.Data.DraftID : _3DProduct.Tracker.Data.ProductID;

            try
            {
                var response = await this.GetStringAndUpdateAuthenticityTokenAsync($"turbosquid/{(isDraft ? "drafts" : "products")}/{id}/edit", cancellationToken);
                _logger.Trace($"Edit request for {(isDraft ? "draft" : "product")} with ID {id} was sent.");
                return response;
            }
            catch { _logger.Error($"Edit request for {(isDraft ? "draft" : "product")} with ID {id} failed."); throw; }
        }
    }
}

static class TurboSquid3DProductExtensions
{
    internal static async Task<TurboSquid._3DProduct> ToTurboSquid3DProductAsync(this RFProduct rfProduct, TurboSquid client, CancellationToken cancellationToken)
    {
        var idea = (RFProduct._3D.Idea_)rfProduct.Idea;
        var metadata = JObject.Parse(File.ReadAllText(idea.Metadata)).ToObject<_3DProduct.Metadata_>(JsonSerializer.CreateDefault(new() { MissingMemberHandling = MissingMemberHandling.Ignore}))!;
        var _3DProduct = new _3DProduct(idea.Path,
                idea.Packages.Select(_ => new _3DModel(_)).ToList(),
                idea.Renders.Select(_ => new _3DProductThumbnail(_)).ToList(),
                []);
        return new TurboSquid._3DProduct(
            _3DProduct,
            new TurboSquid._3DProduct.Metadata__(
                Status(),
                metadata.Title,
                metadata.Description,
                metadata.Tags,
                /*metaFile.Read().Category ?? */await Category(),
                metadata.Polygons,
                metadata.Vertices,
                metadata.PriceSquid,
                License(),
                metadata.Animated,
                metadata.Collection,
                Geometry(),
                metadata.Materials,
                metadata.Rigged,
                metadata.Textures,
                metadata.UVMapped,
                UnwrappedUVs())
            );



        RFProduct._3D.Status Status() => metadata.StatusSquid.ToLowerInvariant() switch
        {
            "draft" => RFProduct._3D.Status.draft,
            "online" => RFProduct._3D.Status.online,
            "none" => RFProduct._3D.Status.none,
            _ => throw new NotImplementedException()
        };

        async Task<Category_> Category()
        {
            var defaultCategory = new Category_("sculpture", 330);
            return await SuggestCategoryAsync(metadata.Category) ?? defaultCategory;


            async Task<Category_?> SuggestCategoryAsync(string category)
            {
                var suggestions = JArray.Parse(
                    await client.GetStringAsync($"features/suggestions?fields%5Btags_and_synonyms%5D={WebUtility.UrlEncode(category)}&assignable=true&assignable_restricted=false&ancestry=1%2F6&limit=25", cancellationToken)
                    );
                if (suggestions.FirstOrDefault() is JToken suggestion &&
                    suggestion["text"]?.Value<string>() is string category_ &&
                    suggestion["id"]?.Value<int>() is int id)
                    return new(category_, id);
                else return null;
            }
        }

        TurboSquid._3DProduct.Metadata__.License_ License() => metadata.License switch
        {
            License_.RoyaltyFree => TurboSquid._3DProduct.Metadata__.License_.royalty_free_all_extended_uses,
            License_.Editorial => TurboSquid._3DProduct.Metadata__.License_.royalty_free_editorial_uses_only,
            _ => throw new NotImplementedException()
        };

        TurboSquid._3DProduct.Metadata__.Geometry_? Geometry() => metadata.Geometry switch
        {
            Geometry_.PolygonalQuadsOnly => TurboSquid._3DProduct.Metadata__.Geometry_.polygonal_quads_only,
            Geometry_.PolygonalQuadsTris => TurboSquid._3DProduct.Metadata__.Geometry_.polygonal_quads_tris,
            Geometry_.PolygonalTrisOnly => TurboSquid._3DProduct.Metadata__.Geometry_.polygonal_tris_only,
            Geometry_.PolygonalNgonsUsed => TurboSquid._3DProduct.Metadata__.Geometry_.polygonal_ngons_used,
            Geometry_.Polygonal => TurboSquid._3DProduct.Metadata__.Geometry_.polygonal,
            Geometry_.Subdivision => TurboSquid._3DProduct.Metadata__.Geometry_.subdivision,
            Geometry_.Nurbs => TurboSquid._3DProduct.Metadata__.Geometry_.nurbs,
            Geometry_.Unknown => TurboSquid._3DProduct.Metadata__.Geometry_.unknown,
            null => null,
            _ => throw new NotImplementedException()
        };

        TurboSquid._3DProduct.Metadata__.UnwrappedUVs_? UnwrappedUVs() => metadata.UnwrappedUVs switch
        {
            UnwrappedUVs_.NonOverlapping => TurboSquid._3DProduct.Metadata__.UnwrappedUVs_.yes_non_overlapping,
            UnwrappedUVs_.Overlapping => TurboSquid._3DProduct.Metadata__.UnwrappedUVs_.yes_overlapping,
            UnwrappedUVs_.Mixed => TurboSquid._3DProduct.Metadata__.UnwrappedUVs_.mixed,
            UnwrappedUVs_.No => TurboSquid._3DProduct.Metadata__.UnwrappedUVs_.no,
            UnwrappedUVs_.Unknown => TurboSquid._3DProduct.Metadata__.UnwrappedUVs_.unknown,
            null => null,
            _ => throw new NotImplementedException()
        };
        // Decouple that shit like metadata and bare _3DProduct.
        // _3DProduct is just a fucking data structure representing a 3D product on the file system.
    }
}
