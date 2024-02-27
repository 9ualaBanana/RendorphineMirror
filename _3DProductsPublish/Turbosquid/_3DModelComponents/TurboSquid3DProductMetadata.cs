using System.Net;
using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid.Upload;
using _3DProductsPublish.Turbosquid.Upload.Processing;
using static _3DProductsPublish._3DProductDS._3DProduct.Metadata_;

namespace _3DProductsPublish.Turbosquid._3DModelComponents;

public partial record TurboSquid3DProductMetadata
{
    public static async Task<TurboSquid3DProductMetadata> ProvideAsync(
        string title,
        string description,
        string category,
        string[] tags,
        int polygons,
        int vertices,
        double price,
        License_ license,
        bool animated = false,
        bool collection = false,
        Geometry_? geometry = default,
        bool materials = false,
        bool rigged = false,
        bool textures = false,
        bool uvMapped = false,
        UnwrappedUVs_? unwrappedUvs = default,
        CancellationToken cancellationToken = default)
    {
        return new(title, description, tags, await Category(), polygons, vertices, price, license, animated, collection, geometry, materials, rigged, textures, uvMapped, unwrappedUvs);


        async Task<Category_> Category()
        {
            var httpClient = new HttpClient() { BaseAddress = TurboSquid.Origin};
            while (true)
            {
                var suggestions = JArray.Parse(
                    await httpClient.GetStringAsync($"features/suggestions?fields%5Btags_and_synonyms%5D={WebUtility.UrlEncode(category)}&assignable=true&assignable_restricted=false&ancestry=1%2F6&limit=25", cancellationToken)
                    );
                if (suggestions.FirstOrDefault() is JToken suggestion &&
                    suggestion["text"]?.Value<string>() is string category_ &&
                    suggestion["id"]?.Value<int>() is int id)
                        return new(category_, id);
            }
        }
    }


    TurboSquid3DProductMetadata(
        string title,
        string description,
        string[] tags,
        Category_ category,
        int polygons,
        int vertices,
        double price,
        License_ license,
        bool animated = false,
        bool collection = false,
        Geometry_? geometry = default,
        bool materials = false,
        bool rigged = false,
        bool textures = false,
        bool uvMapped = false,
        UnwrappedUVs_? unwrappedUvs = default)
    {
        Title = title;
        Description = description;
        Tags = tags;
        Category = category; Features.Add(category.Name, category.ID);
        Polygons = polygons;
        Vertices = vertices;
        Price = price;
        License = license;
        Animated = animated;
        Collection = collection; if (collection) Features.Add(nameof(collection), 30232);
        Geometry = geometry;
        Materials = materials;
        Rigged = rigged;
        Textures = textures;
        UVMapped = uvMapped;
        UnwrappedUVs = unwrappedUvs;
    }

    public string Title { get; }
    public string Description { get; }
    public string[] Tags
    {
        get => _tags;
        init
        {
            if (value.Length < 1) throw new ArgumentOutOfRangeException(
                nameof(value.Length),
                value.Length,
                $"{nameof(TurboSquid3DProductMetadata)} requires at least 1 tag.");

            _tags = value;
        }
    }
    string[] _tags = null!;
    public Category_ Category { get; internal set; }
    public Category_? SubCategory { get; internal set; }
    public int Polygons
    {
        get => _polygons;
        init => _polygons = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(Polygons), "Must be greater than 0");
    }
    int _polygons;
    public int Vertices
    {
        get => _vertices;
        init => _vertices = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(Vertices), "Must be greater than 0");
    }
    int _vertices;
    public double Price { get; }
    public License_ License { get; }
    public bool Animated { get; }
    public bool Collection { get; }
    public Geometry_? Geometry { get; }
    public bool Materials { get; }
    public bool Rigged { get; }
    public bool Textures { get; }
    public bool UVMapped { get; }
    public UnwrappedUVs_? UnwrappedUVs { get; }
    internal Dictionary<string, int> Features { get; } = [];

    public JObject ToProductForm(long draftId)
    {
        var productForm = JObject.FromObject(new
        {
            alpha_channel = false,
            animated = Animated,
            biped = false,
            certifications = Array.Empty<string>(),
            color_depth = 0,
            description = Description,
            display_tags = string.Join(' ', Tags),
            draft_id = draftId.ToString(),
            frame_rate = 0,
            height = (string?)null,
            length = (string?)null,
            license = License.ToString(),
            loopable = false,
            materials = Materials,
            multiple_layers = false,
            name = Title,
            polygons = Polygons.ToString(),
            price = Price.ToString("0.00"),
            rigged = Rigged,
            status = "draft",
            textures = Textures,
            tileable = false,
            uv_mapped = UVMapped,
            vertices = Vertices.ToString(),
            width = (string?)null
        });
        productForm.Add("geometry", Geometry is not null ? new JValue(Geometry.ToString()) : new JValue(0));
        productForm.Add("unwrapped_u_vs", UnwrappedUVs is not null ? new JValue(UnwrappedUVs.ToString()) : new JValue(0));

        return productForm;
    }


    public enum License_
    {
        royalty_free_all_extended_uses,
        royalty_free_editorial_uses_only
    }

    public enum Geometry_
    {
        polygonal_quads_only,
        polygonal_quads_tris,
        polygonal_tris_only,
        polygonal_ngons_used,
        polygonal,
        subdivision,
        nurbs,
        unknown
    }

    public enum UnwrappedUVs_
    {
        yes_non_overlapping,
        yes_overlapping,
        mixed,
        no,
        unknown
    }


    public record Product : IEquatable<_3DProduct<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata>>
    {
        public int id { get; init; } = default!;
        public long? draft_id { get; init; } = default!;
        public string name { get; init; } = default!;
        public string description { get; init; } = default!;
        //public string product_type { get; init; }
        public List<string> tags { get; init; } = default!;
        public double? price { get; init; } = default!;
        public License_? license { get; init; } = default!;
        //public List<string> certifications { get; init; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Geometry_? geometry { get; init; } = default!;
        public int? polygons { get; init; } = default!;
        public int? vertices { get; init; } = default!;
        public bool? materials { get; init; } = default!;
        public bool? rigged { get; init; } = default!;
        public bool? animated { get; init; } = default!;
        [JsonConverter(typeof(StringEnumConverter))]
        public UnwrappedUVs_? unwrapped_u_vs { get; init; } = default!;
        public bool? textures { get; init; } = default!;
        public bool? uv_mapped { get; init; } = default!;
        public List<File> files { get; init; } = default!;
        internal IEnumerable<File> models => files.Where(_ => _.type == "product_file");
        public List<Preview> previews { get; init; } = default!;


        public record File(
            long id,
            string type,
            File.Attributes attributes) : IEquatable<TurboSquidProcessed3DModel>
        {
            public bool Equals(TurboSquidProcessed3DModel? other) =>
                id != other?.FileId ? throw new InvalidDataException() :
                attributes.Equals(other.Metadata);

            public record Attributes(
                string name,
                long size,
                string file_format,
                double? format_version,
                string renderer,
                double? renderer_version,
                bool is_native) : IEquatable<TurboSquid3DModelMetadata>
            {
                public bool Equals(TurboSquid3DModelMetadata? other) =>
                    Path.GetFileNameWithoutExtension(name) == other?.Name &&
                    file_format == other.FileFormat &&
                    format_version == other.FormatVersion &&
                    is_native == other.IsNative &&
                    renderer == other.Renderer &&
                    renderer_version == other.RendererVersion;
            }
        }

        internal static Product Parse(string productPage)
        {
            var featuresIndex = productPage.EndIndexOf(";gon.features=");
            var featuresDefinition = productPage[featuresIndex..productPage.IndexOf(";gon.is_published", featuresIndex)];
            var productIndex = productPage.EndIndexOf("gon.product=");
            var productDefinition = productPage[productIndex..productPage.IndexOf(";gon.features", productIndex)];
            var product = JObject.Parse(productDefinition); product["features"]!.Replace(JArray.Parse(featuresDefinition));

            return product.ToObject<Product>() ?? throw new InvalidDataException();
        }

        public bool Equals(_3DProduct<TurboSquid3DProductMetadata, TurboSquid3DModelMetadata>? other) =>
            id != other?.ID ? throw new InvalidOperationException() :
            name == other?.Metadata.Title &&
            description == other.Metadata.Description &&
            tags.SequenceEqual(other.Metadata.Tags) &&
            price == other.Metadata.Price &&
            geometry == other.Metadata.Geometry &&
            polygons == other.Metadata.Polygons &&
            vertices == other.Metadata.Vertices &&
            materials == other.Metadata.Materials &&
            rigged == other.Metadata.Rigged &&
            animated == other.Metadata.Animated &&
            unwrapped_u_vs == other.Metadata.UnwrappedUVs &&
            textures == other.Metadata.Textures &&
            uv_mapped == other.Metadata.UVMapped &&
            license == other.Metadata.License;
    }

    public record Preview
    {
        public string type { get; init; }
        public long id { get; init; }
        public string filename { get; init; }
        public bool watermarked { get; init; }
        //public string url_64 { get; init; }
        //public string url_90 { get; init; }
        //public string url_128 { get; init; }
        //public string url_200 { get; init; }
        //public string url_600 { get; init; }
        //public string url_1480 { get; init; }
        //public string url_1480_hq { get; init; }
        //public string url_zoom { get; init; }
        public string thumbnail_type { get; init; }
        //public int source_width { get; init; }
        //public int source_height { get; init; }
        //public bool search_background { get; init; }
    }
}
