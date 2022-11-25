using System.Net.Http.Json;

namespace Transport.Upload._3DModelsUpload.CGTrader._3DModelComponents;

public record CGTrader3DModelMetadata : _3DModelMetadata
{
    const double DefaultPrice = 2.0;

    public string Title { get; }
    public string Description { get; }
    public string[] Tags
    {
        get => _tags!;
        private init
        {
            if (value.Length < 5) throw new ArgumentOutOfRangeException(
                nameof(value.Length),
                value.Length,
                $"{nameof(CGTrader3DModelMetadata)} requires at least 5 tags.");

            _tags = value;
        }
    }
    string[]? _tags;
    public int Category { get; }
    public int SubCategory { get; }
    public CGTraderLicense License { get; }
    public string? CustomLicense { get; }
    public bool Free => Price == 0.0;
    public double Price { get; }
    public ProductType ProductType { get; }
    public bool? GameReady { get; }
    public bool? Animated { get; }
    public bool? Rigged { get; }
    public bool? PhysicallyBasedRendering { get; }
    public bool? AdultContent { get; }
    public CGTrader3DModelAdditionalMetadata? Info { get; }
    internal List<string> UploadedPreviewImagesIDs { get; } = new();

    #region Initialization

    public static CGTrader3DModelMetadata ForCG(
     string title,
     string description,
     string[] tags,
     CGTrader3DModelCategory category,
     NonCustomCGTraderLicense license,
     double price = DefaultPrice,
     bool gameReady = false,
     bool animated = false,
     bool rigged = false,
     bool physicallyBasedRendering = false,
     bool adultContent = false,
     CGTrader3DModelAdditionalMetadata? info = null) => new(
         title,
         description,
         tags,
         category,
         Enum.Parse<CGTraderLicense>(license.ToString()),
         price,
         customLicenseText: null,
         ProductType.cg,
         gameReady,
         animated,
         rigged,
         physicallyBasedRendering,
         adultContent,
         info);

    public static CGTrader3DModelMetadata ForCG(
        string title,
        string description,
        string[] tags,
        CGTrader3DModelCategory category,
        string customLicenseText,
        double price = DefaultPrice,
        bool gameReady = false,
        bool animated = false,
        bool rigged = false,
        bool physicallyBasedRendering = false,
        bool adultContent = false,
        CGTrader3DModelAdditionalMetadata? info = null) => new(
            title,
            description,
            tags,
            category,
            CGTraderLicense.custom,
            price,
            customLicenseText,
            ProductType.cg,
            gameReady,
            animated,
            rigged,
            adultContent,
            physicallyBasedRendering,
            info);

    public static CGTrader3DModelMetadata ForPrintable(
        string title,
        string description,
        string[] tags,
        CGTrader3DModelCategory category,
        NonCustomCGTraderLicense license,
        double price = DefaultPrice) => new(
            title,
            description,
            tags,
            category,
            Enum.Parse<CGTraderLicense>(license.ToString()),
            price,
            customLicenseText: null,
            ProductType.printable);

    public static CGTrader3DModelMetadata ForPrintable(
        string title,
        string description,
        string[] tags,
        CGTrader3DModelCategory category,
        string customLicenseText,
        double price = DefaultPrice) => new(
            title,
            description,
            tags,
            category,
            CGTraderLicense.custom,
            price,
            customLicenseText,
            ProductType.printable);

    CGTrader3DModelMetadata(
        string title,
        string description,
        string[] tags,
        CGTrader3DModelCategory category,
        CGTraderLicense license,
        double price = DefaultPrice,
        string? customLicenseText = null,
        ProductType productType = ProductType.cg,
        bool? gameReady = null,
        bool? animated = null,
        bool? rigged = null,
        bool? physicallyBasedRendering = null,
        bool? adultContent = null,
        CGTrader3DModelAdditionalMetadata? info = null)
    {
        Title = title;
        Description = description;
        Tags = tags;
        Category = category.CategoryID;
        SubCategory = category.SubCategoryID;
        License = license;
        Price = price;
        CustomLicense = customLicenseText;
        ProductType = productType;
        GameReady = gameReady;
        Animated = animated;
        Rigged = rigged;
        PhysicallyBasedRendering = physicallyBasedRendering;
        AdultContent = adultContent;
        Info = info;
    }

    #endregion

    /// <exception cref="InvalidOperationException">
    /// <see cref="CGTrader3DModelMetadata"/> doesn't describe a product  with <see cref="ProductType.cg"/> <see cref="ProductType"/>.
    /// </exception>
    internal JsonContent _AsCGJsonContent
    {
        get
        {
            if (ProductType is not ProductType.cg) throw new InvalidOperationException(
                $"{nameof(CGTrader3DModelMetadata)} must describe a product with {nameof(ProductType.cg)} {nameof(ProductType)}"
                );

            return JsonContent.Create(new
            {

                dont_validate = false.ToString(),
                item = new
                {
                    adult_content = AdultContent.ToString(),
                    animated = Animated.ToString(),
                    category_id = Category,
                    custom_license = CustomLicense ?? string.Empty,
                    description = Description,
                    draft = true.ToString(),
                    embed_ids = string.Empty,
                    free = Free.ToString(),
                    game_ready = GameReady.ToString(),
                    geometry_type = Info?.GeometryType?.ToString() ?? null,
                    image_ids = UploadedPreviewImagesIDs,
                    license = License.ToString(),
                    materials = Info?.Materials.ToString() ?? false.ToString(),
                    metaverse_ids = string.Empty,
                    pbr = PhysicallyBasedRendering.ToString(),
                    plugins_used = Info?.PluginsUsed.ToString() ?? false.ToString(),
                    polygons = Info?.Polygons.ToString() ?? false.ToString(),
                    price = Price,
                    removed_image_ids = string.Empty,
                    rigged = Rigged.ToString(),
                    sub_category_id = SubCategory,
                    tags = Tags,
                    textures = Info?.Textures.ToString() ?? false.ToString(),
                    title = Title,
                    type = ProductType.ToString(),
                    unwrapped_uvs = Info?.UnwrappedUVs.ToString() ?? null,
                    uvw_mapping = Info?.UVWMapping.ToString() ?? false.ToString(),
                    vertices = Info?.Vertices
                }
            });
        }
    }
}

public enum NonCustomCGTraderLicense { royalty_free, editorial }

public enum CGTraderLicense { royalty_free, editorial, custom }

public enum ProductType { cg, printable }
