using System.Net.Http.Json;
using static MarkTM.RFProduct.RFProduct._3D;

namespace _3DProductsPublish.CGTrader;

public partial class CGTrader
{
    public partial record _3DProduct
    {
        public partial record Metadata__
        {
            // Make Metadata abstract and inherit cg and printable classes from it.
            public record CG(
                int? Polygons = null,
                int? Vertices = null,
                Geometry_? Geometry = null,  // sent as (null) in JSON
                bool? Collection = null,
                bool? Textures = false,
                bool Materials = true,
                bool PluginsUsed = false,
                bool UVWMapping = false,
                UnwrappedUVs_? UnwrappedUVs = null)  // sent as (null) in JSON
            {
            }

            const double DefaultPrice = 2.0;

            public Status Status { get; set; }
            public string Title { get; }
            public string Description { get; }
            public string[] Tags
            {
                get => _tags;
                private init
                {
                    if (value.Length < 5) throw new ArgumentOutOfRangeException(
                        nameof(value.Length),
                        value.Length,
                        $"{nameof(Metadata__)} requires at least 5 and no more than 20 tags.");

                    _tags = value.Take(20).ToArray();
                }
            }
            string[] _tags = null!;
            public Metadata_.Category_ Category { get; }
            public Metadata_.Category_ SubCategory { get; }
            public License_ License { get; }
            public string? CustomLicense { get; }
            public bool Free => Price == 0;
            public double Price
            {
                get => _price;
                init
                {
                    if (value >= 2 || value == 0)
                        _price = value;
                    else throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        $"Price for CGTrader 3D product must be equal or greater than {DefaultPrice}, or free.");
                }
            }
            double _price;
            public ProductType_ ProductType { get; }
            public bool? GameReady { get; }
            public bool? Animated { get; }
            public bool? Rigged { get; }
            public bool? PhysicallyBasedRendering { get; }
            public bool? AdultContent { get; }
            public int? Polygons { get; }
            public int? Vertices { get; }
            public Geometry_? Geometry { get; }
            public bool? Collection { get; }
            public bool Textures { get; }
            public bool Materials { get; }
            public bool PluginsUsed { get; }
            public bool UVWMapping { get; }
            public UnwrappedUVs_? UnwrappedUVs { get; }

            #region Initialization

            public static Metadata__ ForCG(
                Status status,
                string title,
                string description,
                string[] tags,
                Category_ category,
                NonCustomLicense license,
                double price = DefaultPrice,
                bool gameReady = false,
                bool animated = false,
                bool rigged = false,
                bool physicallyBasedRendering = false,
                bool adultContent = false,
                CG? cg = null) => new(
                    status,
                    title,
                    description,
                    tags,
                    category,
                    Enum.Parse<License_>(license.ToString()),
                    price,
                    customLicenseText: null,
                    ProductType_.cg,
                    gameReady,
                    animated,
                    rigged,
                    physicallyBasedRendering,
                    adultContent,
                    cg);

            public static Metadata__ ForCG(
                Status status,
                string title,
                string description,
                string[] tags,
                Category_ category,
                string customLicenseText,
                double price = DefaultPrice,
                bool gameReady = false,
                bool animated = false,
                bool rigged = false,
                bool physicallyBasedRendering = false,
                bool adultContent = false,
                CG? cg = null) => new(
                    status,
                    title,
                    description,
                    tags,
                    category,
                    License_.custom,
                    price,
                    customLicenseText,
                    ProductType_.cg,
                    gameReady,
                    animated,
                    rigged,
                    adultContent,
                    physicallyBasedRendering,
                    cg);

            public static Metadata__ ForPrintable(
                Status status,
                string title,
                string description,
                string[] tags,
                Category_ category,
                NonCustomLicense license,
                double price = DefaultPrice) => new(
                    status,
                    title,
                    description,
                    tags,
                    category,
                    Enum.Parse<License_>(license.ToString()),
                    price,
                    customLicenseText: null,
                    ProductType_.printable);

            public static Metadata__ ForPrintable(
                Status status,
                string title,
                string description,
                string[] tags,
                Category_ category,
                string customLicenseText,
                double price = DefaultPrice) => new(
                    status,
                    title,
                    description,
                    tags,
                    category,
                    License_.custom,
                    price,
                    customLicenseText,
                    ProductType_.printable);

            Metadata__(
                Status status,
                string title,
                string description,
                string[] tags,
                Category_ category,
                License_ license,
                double price = DefaultPrice,
                string? customLicenseText = null,
                ProductType_ productType = ProductType_.cg,
                bool? gameReady = null,
                bool? animated = null,
                bool? rigged = null,
                bool? physicallyBasedRendering = null,
                bool? adultContent = null,
                CG? cg = null)
            {
                Status = status;
                Title = title;
                Description = description;
                Tags = tags;
                Category = category.Category;
                SubCategory = category.SubCategory;
                License = license;
                Price = price;
                CustomLicense = customLicenseText;
                ProductType = productType;
                GameReady = gameReady;
                Animated = animated;
                Rigged = rigged;
                PhysicallyBasedRendering = physicallyBasedRendering;
                AdultContent = adultContent;
                Polygons = cg.Polygons;
                Vertices = cg?.Vertices;
                Geometry = cg?.Geometry;
                Collection = cg?.Collection;
                Textures = cg?.Textures ?? false;
                Materials = cg?.Materials ?? false;
                PluginsUsed = cg?.PluginsUsed ?? false;
                UVWMapping = cg?.UVWMapping ?? false;
                UnwrappedUVs = cg?.UnwrappedUVs;
            }

            //[JsonConstructor]
            //[Obsolete("Only for json deserializing")]
            //Metadata__(string title,
            //    string description,
            //    string[] tags,
            //    int category,
            //    int subCategory,
            //    License_ license,
            //    string? customLicense,
            //    double price,
            //    ProductType_ productType,
            //    bool? gameReady = null,
            //    bool? animated = null,
            //    bool? rigged = null,
            //    bool? physicallyBasedRendering = null,
            //    bool? adultContent = null,
            //    int? polygons = null,
            //    int? vertices = null,
            //    Geometry_? geometry = null,
            //    bool? collection = null,
            //    bool textures = false,
            //    bool materials = false,
            //    bool pluginsUsed = false,
            //    bool uvwMapping = false,
            //    UnwrappedUVs_? unwrappedUVs = null)
            //{
            //    Title = title;
            //    Description = description;
            //    Tags = tags;
            //    Category = new(null, category);
            //    SubCategory = new(null, subCategory);
            //    License = license;
            //    CustomLicense = customLicense;
            //    Price = price;
            //    ProductType = productType;
            //    GameReady = gameReady;
            //    Animated = animated;
            //    Rigged = rigged;
            //    PhysicallyBasedRendering = physicallyBasedRendering;
            //    AdultContent = adultContent;
            //    Polygons = polygons;
            //    Vertices = vertices;
            //    Geometry = geometry;
            //    Collection = collection;
            //    Textures = textures;
            //    Materials = materials;
            //    PluginsUsed = pluginsUsed;
            //    UVWMapping = uvwMapping;
            //    UnwrappedUVs = unwrappedUVs;
            //}

            #endregion

            internal JsonContent ToProductForm(IEnumerable<string> image_ids, IEnumerable<string>? removed_image_ids = default)
            {
                if (ProductType is not ProductType_.cg) throw new InvalidOperationException(
                    $"{nameof(Metadata__)} must describe a product with {nameof(ProductType.cg)} {nameof(ProductType)}"
                    );
                removed_image_ids ??= [];

                return JsonContent.Create(new
                {

                    dont_validate = (removed_image_ids is not null && removed_image_ids.Any()).ToString(),
                    item = new
                    {
                        adult_content = AdultContent.ToString(),
                        animated = Animated.ToString(),
                        category_id = Category.ID,
                        custom_license = CustomLicense ?? string.Empty,
                        description = Description,
                        draft = true.ToString(),
                        embed_ids = string.Empty,
                        free = Free.ToString(),
                        game_ready = GameReady.ToString(),
                        geometry_type = Geometry?.ToString() ?? null,
                        image_ids,
                        license = License.ToString(),
                        materials = Materials.ToString() ?? false.ToString(),
                        metaverse_ids = string.Empty,
                        pbr = PhysicallyBasedRendering.ToString(),
                        plugins_used = PluginsUsed.ToString() ?? false.ToString(),
                        polygons = Polygons.ToString() ?? false.ToString(),
                        price = Price,
                        removed_image_ids,
                        rigged = Rigged.ToString(),
                        sub_category_id = SubCategory.ID,
                        tags = Tags,
                        textures = Textures.ToString() ?? false.ToString(),
                        title = Title,
                        type = ProductType.ToString(),
                        unwrapped_uvs = UnwrappedUVs.ToString() ?? null,
                        uvw_mapping = UVWMapping.ToString() ?? false.ToString(),
                        vertices = Vertices
                    }
                });
            }

            public enum NonCustomLicense { royalty_free, editorial }

            public enum License_ { royalty_free, editorial, custom }

            public enum ProductType_ { cg, printable }

            public enum Geometry_
            {
                polygon_mesh,
                subdivision_ready,
                nurbs,
                other
            }

            public enum UnwrappedUVs_
            {
                unknown,
                non_overlapping,
                overlapping,
                mixed,
                no
            }
        }
    }
}
