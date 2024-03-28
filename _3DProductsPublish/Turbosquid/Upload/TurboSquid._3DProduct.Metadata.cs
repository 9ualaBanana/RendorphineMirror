using MarkTM.RFProduct;
using static _3DProductsPublish._3DProductDS._3DProduct.Metadata_;

namespace _3DProductsPublish.Turbosquid.Upload;

public partial class TurboSquid
{
    public partial record _3DProduct
    {
        public partial record Metadata__
        {
            internal Metadata__(
                RFProduct._3D.Status status,
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
                Status = status;
                Title = title.Trim();
                Description = description.Trim();
                Tags = tags.Select(_ => _.Trim()).ToArray();
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

            public RFProduct._3D.Status Status { get; }
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
                        $"{nameof(Metadata__)} requires at least 1 tag.");

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

            public JObject ToProductForm(long draftId, string status)
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
                    status,
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
        }
    }
}
