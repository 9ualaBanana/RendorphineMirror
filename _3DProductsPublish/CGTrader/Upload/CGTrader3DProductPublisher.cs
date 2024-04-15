using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.CGTrader._3DModelComponents;
using _3DProductsPublish.CGTrader.Api;
using MarkTM.RFProduct;
using System.Net;

namespace _3DProductsPublish.CGTrader.Upload;

public class CGTrader3DProductPublisher(CGTraderApi api)
{
    readonly CGTraderApi _api = api;

    public async Task PublishAsync(RFProduct rfProduct, NetworkCredential credential, CancellationToken cancellationToken)
        => await PublishAsync(rfProduct.ToCGTrader3DProductAsync(), credential, cancellationToken);
    public async Task PublishAsync(
        _3DProduct<CGTrader3DProductMetadata> _3DModel,
        NetworkCredential credential,
        CancellationToken cancellationToken)
    {
        var sessionContext = await _api.RequestSessionContextAsync(new(credential.UserName, credential.Password, true), cancellationToken);

        await _api.LoginAsync(sessionContext, cancellationToken);
        var modelDraft = await _api.CreateNewModelDraftAsyncFor(_3DModel, sessionContext, cancellationToken);
        await _api._UploadAssetsAsync(modelDraft, cancellationToken);
        await _api._UploadMetadataAsync(modelDraft, cancellationToken);
        await _api._PublishAsync(modelDraft, cancellationToken);
    }
}

static class CGTrader3DProductExtensions
{
    public static _3DProduct<CGTrader3DProductMetadata> ToCGTrader3DProductAsync(this RFProduct rfProduct)
    {
        var idea = (RFProduct._3D.Idea_)rfProduct.Idea;
        var metadata = JObject.Parse(File.ReadAllText(idea.Metadata)).ToObject<_3DProduct.Metadata_>(JsonSerializer.CreateDefault(new() { MissingMemberHandling = MissingMemberHandling.Ignore }))!;
        var _3DProduct = new _3DProduct(idea.Path,
                idea.Packages.Select(_ => new _3DModel(_)).ToList(),
                idea.Renders.Select(_ => new _3DProductThumbnail(_)).ToList(),
                []);
        var cg3DProduct = new _3DProduct<CGTrader3DProductMetadata>(
            _3DProduct,
            CGTrader3DProductMetadata.ForCG(
                metadata.Title,
                metadata.Description,
                metadata.Tags,
                Category(),
                License(),
                metadata.PriceTrader,
                metadata.GameReady,
                metadata.Animated,
                metadata.Rigged,
                metadata.PhysicallyBasedRendering,
                metadata.AdultContent,
                new(metadata.Polygons, metadata.Vertices, Geometry(), metadata.Collection, metadata.Textures, metadata.Materials, metadata.PluginsUsed, metadata.UVMapped, UnwrappedUVs()))
            );
        return cg3DProduct;
        
        
        CGTrader3DProductCategory Category()
        {
            
            return CGTrader3DProductCategory.Electoronics(ElectronicsSubCategory.Computer);
            throw new NotImplementedException();
        }

        NonCustomCGTraderLicense License() => metadata.License switch
        {
            _3DProduct.Metadata_.License_.RoyaltyFree => NonCustomCGTraderLicense.royalty_free,
            _3DProduct.Metadata_.License_.Editorial => NonCustomCGTraderLicense.editorial,
            _ => throw new NotImplementedException()
        };

        Geometry_? Geometry() => metadata.Geometry switch
        {
            _3DProduct.Metadata_.Geometry_.PolygonalQuadsOnly or
            _3DProduct.Metadata_.Geometry_.PolygonalQuadsTris or
            _3DProduct.Metadata_.Geometry_.PolygonalTrisOnly or
            _3DProduct.Metadata_.Geometry_.PolygonalNgonsUsed or
            _3DProduct.Metadata_.Geometry_.Polygonal => Geometry_.polygonal_mesh,
            _3DProduct.Metadata_.Geometry_.Subdivision => Geometry_.subdivision_ready,
            _3DProduct.Metadata_.Geometry_.Nurbs => Geometry_.nurbs,
            _3DProduct.Metadata_.Geometry_.Unknown => Geometry_.other,
            null => null,
            _ => throw new NotImplementedException()
        };

        UnwrappedUVs_? UnwrappedUVs() => metadata.UnwrappedUVs switch
        {
            _3DProduct.Metadata_.UnwrappedUVs_.NonOverlapping => UnwrappedUVs_.non_overlapping,
            _3DProduct.Metadata_.UnwrappedUVs_.Overlapping => UnwrappedUVs_.overlapping,
            _3DProduct.Metadata_.UnwrappedUVs_.Mixed => UnwrappedUVs_.mixed,
            _3DProduct.Metadata_.UnwrappedUVs_.No => UnwrappedUVs_.no,
            _3DProduct.Metadata_.UnwrappedUVs_.Unknown => UnwrappedUVs_.unknown,
            null => null,
            _ => throw new NotImplementedException()
        };
    }
}
