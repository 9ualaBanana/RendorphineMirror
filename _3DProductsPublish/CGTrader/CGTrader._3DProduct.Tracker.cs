using _3DProductsPublish._3DProductDS;
using static _3DProductsPublish._3DProductDS._3DProduct.Metadata_;
using static MarkTM.RFProduct.RFProduct._3D;

namespace _3DProductsPublish.CGTrader;

public partial class CGTrader
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
            internal const string Name = "cgtrader.json";
            internal string Path => _path ??= System.IO.Path.Combine(_3DProduct.ContainerPath, Name);
            string? _path;

            internal Target<_3DModelMetadata> Model(_3DModel _3DModel)
                => Data.Models.Single(tracker => tracker._.Name == _3DModel.Name());

            internal Target<CGTraderAssetMetadata> Preview(_3DProductThumbnail preview)
                => Data.Previews.Single(tracker => tracker._.Name == preview.Name());

            internal Data_ Read()
            {
                var content = File.ReadAllText(Path).Trim();
                if (content.Length is 0)
                { Write(); content = File.ReadAllText(Path).Trim(); }

                Data = JsonConvert.DeserializeObject<Data_>(content) ?? throw new InvalidDataException();
                var models = _3DProduct._3DModels.Select(_3DModel => new Target<_3DModelMetadata>(new(_3DModel), _3DModel.Path));
                Data.Models.IntersectWith(models); Data.Models.UnionWith(models);
                var previews = _3DProduct.Thumbnails.Select(preview => new Target<CGTraderAssetMetadata>(new(preview), preview.Path));
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
                file.Create().Dispose();
                Data = Read();
            }


            public record Data_
            {
                internal Data_()
                    : this(default, default, default, [], []) { }
                public Data_(long draftId, Category_? category, Category_? subCategory, HashSet<Target<_3DModelMetadata>> models, HashSet<Target<CGTraderAssetMetadata>> previews)
                {
                    DraftID = draftId; 
                    Category = category;
                    SubCategory = subCategory;
                    Models = models;
                    Previews = previews;
                }
                [JsonConverter(typeof(StringEnumConverter))]
                public Status Status { get; set; }
                public long DraftID { get; set; }
                public Category_? Category { get; set; }
                public Category_? SubCategory { get; set; }
                public HashSet<Target<_3DModelMetadata>> Models { get; set; }
                public HashSet<Target<CGTraderAssetMetadata>> Previews { get; set; }
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

            public record CGTraderAssetMetadata
            {
                public string Name { get; init; }

                public CGTraderAssetMetadata(I3DProductAsset asset)
                    : this(asset.Name())
                {
                }
                [JsonConstructor]
                public CGTraderAssetMetadata(string name)
                {
                    Name = name;
                }
            }
        }
    }
}
