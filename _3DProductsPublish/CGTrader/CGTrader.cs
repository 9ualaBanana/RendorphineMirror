using System.Net;
using _3DProductsPublish._3DProductDS;
using MarkTM.RFProduct;
using static _3DProductsPublish._3DProductDS._3DProduct.Metadata_;
using static MarkTM.RFProduct.RFProduct._3D;
using System.Collections.Concurrent;
using _3DProductsPublish.CGTrader.Captcha;

namespace _3DProductsPublish.CGTrader;

public partial class CGTrader : HttpClient, I3DStock<CGTrader>
{
    // Use Tracker for manipulating asset synchronization status (local, online, updated).
    // local - exists in local storage, not uploaded
    // online - uploaded to remote
    // updated - local asset is newer than remote asset
    
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    internal static Uri Origin { get; } = new("https://www.cgtrader.com");

    public static async Task<CGTrader> LogInAsyncUsing(NetworkCredential credential, INodeGui gui, CancellationToken cancellationToken)
    {
        var handler = new SocketsHttpHandler();
        return new(handler, await LoginAsync());


        async Task<string> LoginAsync()
        {
            try
            {
                var authenticityToken = await LoginAsyncCore();
                _logger.Debug("{User} is successfully logged in.", credential.UserName);
                return authenticityToken;
            }
            catch (HttpRequestException ex)
            {
                string errorMessage = $"Login attempt for {credential.UserName} was unsuccessful.";
                _logger.Error(ex, errorMessage);
                throw new HttpRequestException(errorMessage, ex, ex.StatusCode);
            }


            async Task<string> LoginAsyncCore()
            {
                var client = new HttpClient(handler) { BaseAddress = Origin };
                string documentWithSessionCredentials = (await client.GetStringAsync("load-services.js", cancellationToken))
                    .ReplaceLineEndings(string.Empty);
                string authenticityToken = AuthenticityToken.ParseFromJS(documentWithSessionCredentials);
                string siteKey = CGTraderCaptchaSiteKey.Parse(documentWithSessionCredentials);
                string verifiedToken = await new Captcha(gui).SolveCaptchaAsync(siteKey, cancellationToken);
                await
                (await client.PostAsync("users/2fa-or-login.json",
                new MultipartFormDataContent()
                {
                    { new StringContent(authenticityToken), "authenticity_token" },
                    { new StringContent(verifiedToken), "user[MTCaptchaToken]" },
                    { new StringContent("/"), "location" },
                    { new StringContent(credential.UserName), "user[login]" },
                    { new StringContent(credential.Password), "user[password]" },
                    { new StringContent("on"), "user[remember_me]" }, // on/off
                    { new StringContent(verifiedToken), "mtcaptcha-verifiedtoken" }
                },
                cancellationToken))
                .EnsureSuccessStatusCodeAsync(cancellationToken);

                return authenticityToken;
            }
        }
    }
    CGTrader(HttpMessageHandler handler, string authenticityToken)
        : base(handler)
    {
        BaseAddress = Origin;
        DefaultRequestHeaders.Add(AuthenticityToken.Header, authenticityToken);
    }

    readonly ConcurrentDictionary<string, ManualResetEventSlim> _productsLocker = [];
    public async Task UploadAsync(RFProduct rfProduct, CancellationToken cancellationToken)
    {
        var productUploadEvent = _productsLocker.GetOrAdd(rfProduct.Name, new ManualResetEventSlim(true));
        try
        {
            if (!productUploadEvent.Wait(TimeSpan.FromMinutes(10), cancellationToken))
                throw new OperationCanceledException("Timeout waiting for product upload is expired.");
            productUploadEvent.Reset();   // Acquire lock before metadata is read to detect its modifications.
            var _3DProduct = rfProduct.ToCGTrader3DProductAsync();

            if (_3DProduct.Metadata.Status is Status.none)
            { _logger.Warn($"{_3DProduct.Metadata.Title} 3D {nameof(RFProduct)} is not uploaded due to its status being set to {Status.none}."); return; }

            var draftSession = await DraftSession.InitializeAsync(_3DProduct, this, cancellationToken);
            await draftSession.UploadAssetsAsync();
            await draftSession.UploadMetadataAsync();
            if (_3DProduct.Metadata.Status is Status.online)
                await draftSession.PublishAsync();
        }
        finally { productUploadEvent.Set(); }
    }
}

static class CGTraderExtensions
{
    public static CGTrader._3DProduct ToCGTrader3DProductAsync(this RFProduct rfProduct)
    {
        var idea = (RFProduct._3D.Idea_)rfProduct.Idea;
        var metadata = JObject.Parse(File.ReadAllText(idea.Metadata)).ToObject<_3DProduct.Metadata_>(JsonSerializer.CreateDefault(new() { MissingMemberHandling = MissingMemberHandling.Ignore }))!;
        var _3DProduct = new _3DProduct(idea.Path,
                idea.Packages.Select(_ => new _3DModel(_)).ToList(),
                idea.Renders.Select(_ => new _3DProductThumbnail(_)).ToList(),
                []);
        var cg3DProduct = new CGTrader._3DProduct(
            _3DProduct,
            CGTrader._3DProduct.Metadata__.ForCG(
                metadata.StatusTrader,
                metadata.Title,
                metadata.Description,
                metadata.Tags,
                CGTrader._3DProduct.Metadata__.Category_.Parse(metadata.Category),
                License(),
                metadata.PriceTrader,
                metadata.GameReady,
                metadata.Animated,
                metadata.Rigged,
                metadata.PhysicallyBasedRendering,
                metadata.AdultContent,
                new(metadata.Polygons, metadata.Vertices, Geometry(), metadata.Collection, metadata.Textures, metadata.Materials, metadata.PluginsUsed, metadata.UVMapped, UnwrappedUVs()))
            );
        cg3DProduct.Tracker.Write();
        return cg3DProduct;


        CGTrader._3DProduct.Metadata__.NonCustomLicense License() => metadata.License switch
        {
            License_.RoyaltyFree => CGTrader._3DProduct.Metadata__.NonCustomLicense.royalty_free,
            License_.Editorial => CGTrader._3DProduct.Metadata__.NonCustomLicense.editorial,
            _ => throw new NotImplementedException()
        };

        CGTrader._3DProduct.Metadata__.Geometry_? Geometry() => metadata.Geometry switch
        {
            Geometry_.PolygonalQuadsOnly or
            Geometry_.PolygonalQuadsTris or
            Geometry_.PolygonalTrisOnly or
            Geometry_.PolygonalNgonsUsed or
            Geometry_.Polygonal => CGTrader._3DProduct.Metadata__.Geometry_.polygon_mesh,
            Geometry_.Subdivision => CGTrader._3DProduct.Metadata__.Geometry_.subdivision_ready,
            Geometry_.Nurbs => CGTrader._3DProduct.Metadata__.Geometry_.nurbs,
            Geometry_.Unknown => CGTrader._3DProduct.Metadata__.Geometry_.other,
            null => null,
            _ => throw new NotImplementedException()
        };

        CGTrader._3DProduct.Metadata__.UnwrappedUVs_? UnwrappedUVs() => metadata.UnwrappedUVs switch
        {
            UnwrappedUVs_.NonOverlapping => CGTrader._3DProduct.Metadata__.UnwrappedUVs_.non_overlapping,
            UnwrappedUVs_.Overlapping => CGTrader._3DProduct.Metadata__.UnwrappedUVs_.overlapping,
            UnwrappedUVs_.Mixed => CGTrader._3DProduct.Metadata__.UnwrappedUVs_.mixed,
            UnwrappedUVs_.No => CGTrader._3DProduct.Metadata__.UnwrappedUVs_.no,
            UnwrappedUVs_.Unknown => CGTrader._3DProduct.Metadata__.UnwrappedUVs_.unknown,
            null => null,
            _ => throw new NotImplementedException()
        };
    }

    internal static async Task EnsureSuccessStatusCodeAsync(this HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var response_ = JObject.Parse(await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancellationToken));
        if ((bool)response_["success"]! is not true)
            throw new HttpRequestException("The value of `success` field in the response is not `true`.");
    }

    internal static HttpRequestMessage WithHostHeader(this HttpRequestMessage request)
    { request.Headers.Host = CGTrader.Origin.Host; return request; }
}
