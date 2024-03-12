using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Upload.Processing;
using MarkTM.RFProduct;

namespace _3DProductsPublish.Turbosquid.Upload;

public partial class TurboSquid
{
    public partial record _3DProduct
    {
        public record Remote : IEquatable<_3DProduct>
        {
            internal static Remote Parse(string productPage)
            {
                var featuresIndex = productPage.EndIndexOf(";gon.features=");
                var featuresDefinition = productPage[featuresIndex..productPage.IndexOf(";gon.is_published", featuresIndex)];
                var productIndex = productPage.EndIndexOf("gon.product=");
                var productDefinition = productPage[productIndex..productPage.IndexOf(";gon.features", productIndex)];
                var product = JObject.Parse(productDefinition); product["features"]!.Replace(JArray.Parse(featuresDefinition));

                return product.ToObject<Remote>() ?? throw new InvalidDataException();
            }

            public int id { get; init; } = default!;
            public long? draft_id { get; init; } = default!;
            [JsonConverter(typeof(StringEnumConverter))]
            public RFProduct._3D.Status status { get; init; } = default!;
            public string name { get; init; } = default!;
            public string description { get; init; } = default!;
            //public string product_type { get; init; }
            public List<string> tags { get; init; } = default!;
            public double? price { get; init; } = default!;
            public Metadata__.License_? license { get; init; } = default!;
            //public List<string> certifications { get; init; }
            [JsonConverter(typeof(StringEnumConverter))]
            public Metadata__.Geometry_? geometry { get; init; } = default!;
            public int? polygons { get; init; } = default!;
            public int? vertices { get; init; } = default!;
            public bool? materials { get; init; } = default!;
            public bool? rigged { get; init; } = default!;
            public bool? animated { get; init; } = default!;
            [JsonConverter(typeof(StringEnumConverter))]
            public Metadata__.UnwrappedUVs_? unwrapped_u_vs { get; init; } = default!;
            public bool? textures { get; init; } = default!;
            public bool? uv_mapped { get; init; } = default!;
            public List<File> files { get; init; } = default!;
            internal IEnumerable<File> models => files.Where(_ => _.type == "product_file");
            internal IEnumerable<File> texture_files => files.Where(_ => _.type == "texture_file");
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


            public bool Equals(_3DProduct? other) =>
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
    }
}
