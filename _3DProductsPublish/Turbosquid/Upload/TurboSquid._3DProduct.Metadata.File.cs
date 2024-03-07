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
            internal record Serialized(long ProductID, long DraftID, Category_ Category, Category_? SubCategory, IEnumerable<TurboSquid3DModelMetadata> Models);

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
                    if (!System.IO.File.Exists(Path))
                        System.IO.File.Create(Path).Dispose();
                }

                internal Serialized Read()
                {
                    var content = System.IO.File.ReadAllText(Path)/*.Trim()*/;
                    if (content.Length is 0)
                    {
                        System.IO.File.WriteAllText(Path, JsonConvert.SerializeObject(
                            new Serialized(
                                _3DProduct.ID,
                                _3DProduct.DraftID,
                                _3DProduct.Metadata.Category,
                                _3DProduct.Metadata.SubCategory,
                                ((_3DProductDS._3DProduct)_3DProduct)._3DModels.Select(_ => new TurboSquid3DModelMetadata(_))),
                            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented }));
                        content = System.IO.File.ReadAllText(Path);
                    }
                    var meta = JsonConvert.DeserializeObject<Serialized>(content) ?? throw new InvalidDataException();
                    _3DProduct._3DModels = ((_3DProductDS._3DProduct)_3DProduct)._3DModels.Join(meta.Models,
                        _3DModel => _3DModel.Name,
                        metadata => metadata.Name,  // IMetadata.Name is used here.
                        (_3DModel, metadata) => new _3DModel<TurboSquid3DModelMetadata>(_3DModel, metadata))
                        .ToList();
                    if (meta.Models.Count() == _3DProduct._3DModels.Count)
                        if (meta.Models.Count(_ => _.IsNative) is 1)
                            return meta;
                        else throw new InvalidDataException($"Metadata file must mark one {nameof(_3DModel)} as native.");
                    else if (meta.Models.Count() is 0 && _3DProduct._3DModels.Count is 1)
                        return meta with { Models = _3DProduct._3DModels.Select(_ => new TurboSquid3DModelMetadata(_)) };
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
                            _3DProduct._3DModels.Select(_ => _.Metadata)),
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented }));
            }
        }
    }
}
