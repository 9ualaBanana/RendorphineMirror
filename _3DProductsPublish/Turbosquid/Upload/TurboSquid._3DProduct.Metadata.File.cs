using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using static _3DProductsPublish._3DProductDS._3DProduct.Metadata_;

namespace _3DProductsPublish.Turbosquid.Upload;

public partial class TurboSquid
{
    public partial record _3DProduct
    {
        public partial record Metadata__
        {
            internal record Serialized(long ProductID, long DraftID, Category_ Category, Category_? SubCategory, IEnumerable<TurboSquid3DModelMetadata> Models, IEnumerable<TurboSquidPreviewMetadata> Previews);

            public record TurboSquidPreviewMetadata
            {
                public string Name { get; init; }
                public DateTime LastWriteTime { get; init; }

                public TurboSquidPreviewMetadata(_3DProductThumbnail thumbnail)
                    : this(thumbnail.Name(), thumbnail.LastWriteTime)
                {
                }
                [JsonConstructor]
                public TurboSquidPreviewMetadata(string name, DateTime lastWriteTime)
                {
                    Name = name;
                    LastWriteTime = lastWriteTime;
                }
            }

            internal class File
            {
                internal const string Name = "meta.json";
                internal string Path => _path ??= System.IO.Path.Combine(_3DProduct.ContainerPath, Name);
                string? _path;
                internal readonly _3DProduct _3DProduct;

                internal static File For(_3DProduct _3DProduct) => new(_3DProduct);
                File(_3DProduct _3DProduct)
                {
                    this._3DProduct = _3DProduct;
                    if (new FileInfo(Path) is FileInfo file && !file.Exists)
                        file.Create().Dispose();
                }

                internal Serialized Read()
                {
                    var content = System.IO.File.ReadAllText(Path).Trim();
                    if (content.Length is 0)
                    { Update(); content = System.IO.File.ReadAllText(Path).Trim(); }

                    var meta = JsonConvert.DeserializeObject<Serialized>(content) ?? throw new InvalidDataException();
                    if (meta.Models.Count() == ((_3DProductDS._3DProduct)_3DProduct)._3DModels.Count)
                        if (meta.Models.Count(_ => _.IsNative) is 1)
                            return meta;
                        else throw new InvalidDataException($"Metadata file must mark exactly one {nameof(_3DModel)} as native.");
                    else
                    { System.IO.File.Delete(Path); throw new InvalidDataException($"Metadata file doesn't describe every model of {nameof(_3DProduct)} ({Path})."); }
                }

                internal void Update()
                    => System.IO.File.WriteAllText(Path, JsonConvert.SerializeObject(
                        new Serialized(
                            _3DProduct.ID,
                            _3DProduct.DraftID,
                            _3DProduct.Metadata.Category,
                            _3DProduct.Metadata.SubCategory,
                            // Default TurboSquid3DModelMetadata might mark multiple models as native if their format is appropriate.
                            _3DProduct._3DModels?.Select(_ => _.Metadata) ?? ((_3DProductDS._3DProduct)_3DProduct)._3DModels.Select(_ => new TurboSquid3DModelMetadata(_)),
                            _3DProduct.Thumbnails.Select(_ => new TurboSquidPreviewMetadata(_))),
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented }));
            }
        }
    }
}
