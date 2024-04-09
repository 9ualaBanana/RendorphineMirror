using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using static _3DProductsPublish._3DProductDS._3DProduct.Metadata_;
using static MarkTM.RFProduct.RFProduct._3D;

namespace _3DProductsPublish.Turbosquid.Upload;

public partial class TurboSquid
{
    public partial record _3DProduct
    {
        public class Tracker_
        {
            public Tracker_(_3DProductDS._3DProduct _3DProduct)
            {
                this._3DProduct = _3DProduct;
                if (new FileInfo(Path) is FileInfo file && !file.Exists)
                    file.Create().Dispose();
                Data = Read();
            }

            public Data_ Data { get; private set; }
            readonly _3DProductDS._3DProduct _3DProduct;
            internal const string Name = "meta.json";
            internal string Path => _path ??= System.IO.Path.Combine(_3DProduct.ContainerPath, Name);
            string? _path;

            internal Target<TurboSquid3DModelMetadata> Model(_3DModel _3DModel)
                => Data.Models.Single(tracker => tracker._.Name == _3DModel.Name());

            internal Target<TurboSquidAssetMetadata> Preview(_3DProductThumbnail preview)
                => Data.Previews.Single(tracker => tracker._.Name == preview.Name());

            internal Data_ Read()
            {
                var content = File.ReadAllText(Path).Trim();
                if (content.Length is 0)
                { Write(); content = File.ReadAllText(Path).Trim(); }

                Data = JsonConvert.DeserializeObject<Data_>(content) ?? throw new InvalidDataException();
                var models = _3DProduct._3DModels.Select(_3DModel => new Target<TurboSquid3DModelMetadata>(new(_3DModel), _3DModel.Path));
                Data.Models.IntersectWith(models); Data.Models.UnionWith(models);
                var previews = _3DProduct.Thumbnails.Select(preview => new Target<TurboSquidAssetMetadata>(new(preview), preview.Path));
                Data.Previews.IntersectWith(previews); Data.Previews.UnionWith(previews);
                if (Data.Models.Count == _3DProduct._3DModels.Count)
                    if (Data.Models.Count(tracker => tracker._.IsNative) is 1)
                    { Write(); return Data; }
                    else throw new InvalidDataException($"Metadata file must mark exactly one {nameof(_3DModel)} as native.");
                else
                { File.Delete(Path); throw new InvalidDataException($"Metadata file doesn't describe every model of {nameof(_3DProduct)} ({Path})."); }
            }

            internal void Write()
                => File.WriteAllText(Path, JsonConvert.SerializeObject(Data ?? new Data_(),
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented }));

            internal void Reset()
            {
                var file = new FileInfo(Path);
                file.Delete();
                file.Create();
                Data = Read();
            }


            public record Data_
            {
                internal Data_()
                    : this(default, default, default, default, [], []) { }
                public Data_(long productId, long draftId, Category_? category, Category_? subCategory, HashSet<Target<TurboSquid3DModelMetadata>> models, HashSet<Target<TurboSquidAssetMetadata>> previews)
                {
                    ProductID = productId;
                    DraftID = draftId;
                    Category = category;
                    SubCategory = subCategory;
                    Models = models;
                    Previews = previews;
                }
                [JsonConverter(typeof(StringEnumConverter))]
                public Status Status { get; set; }
                public string? Product => ProductID is not 0 ? $"https://www.turbosquid.com/FullPreview/{ProductID}" : null;
                public long ProductID { get; set; }
                public long DraftID { get; set; }
                public Category_? Category { get; set; }
                public Category_? SubCategory { get; set; }
                public HashSet<Target<TurboSquid3DModelMetadata>> Models { get; set; }
                public HashSet<Target<TurboSquidAssetMetadata>> Previews { get; set; }
            }

            public class Target<T> : IEquatable<Target<T>> where T : class
            {
                public T _ { get; init; }
                public long ID { get; set; }
                public DateTime LastWriteTime { get; set; }
                public void Touch() => LastWriteTime = DateTime.UtcNow;
                public void Update(long id)
                { ID = id; Touch(); }

                public Target(T target, string path, long id = default)
                    : this(target, File.GetLastWriteTimeUtc(path), id)
                {
                }
                [JsonConstructor]
                Target(T _, DateTime lastWriteTime, long id)
                {
                    this._ = _;
                    ID = id;
                    LastWriteTime = lastWriteTime;
                }

                public override bool Equals(object? obj) => Equals(obj as Target<T>);
                public bool Equals(Target<T>? other) => _.Equals(other?._);
                public override int GetHashCode() => HashCode.Combine(_);
            }

            public record TurboSquidAssetMetadata
            {
                public string Name { get; init; }

                public TurboSquidAssetMetadata(I3DProductAsset asset)
                    : this(asset.Name())
                {
                }
                [JsonConstructor]
                public TurboSquidAssetMetadata(string name)
                {
                    Name = name;
                }
            }
        }
    }
}
