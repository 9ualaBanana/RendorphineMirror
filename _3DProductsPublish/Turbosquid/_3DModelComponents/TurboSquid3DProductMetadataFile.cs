using System.Reflection;
using _3DProductsPublish._3DProductDS;
using Tomlyn;
using Tomlyn.Syntax;
using static Tomlyn.Helpers.TomlNamingHelper;

namespace _3DProductsPublish.Turbosquid._3DModelComponents;

public partial record TurboSquid3DProductMetadata
{
    internal class File
    {
        internal const string Name = "turbosquid.meta";
        internal string Path => _path ??= System.IO.Path.Combine(_3DProduct.ContainerPath, Name);
        string? _path;
        internal readonly _3DProduct<TurboSquid3DProductMetadata> _3DProduct;

        internal static File For(_3DProduct<TurboSquid3DProductMetadata> _3DProduct) => new(_3DProduct);
        File(_3DProduct<TurboSquid3DProductMetadata> _3DProduct)
        { this._3DProduct = _3DProduct; }

        internal void Populate(INodeGui nodeGui)
        {
            using var _file = System.IO.File.OpenWrite(Path);
            using var file = new StreamWriter(_file);

            var infos = requestInfo(nodeGui, _3DProduct._3DModels.ToArray()).Result.ThrowIfError();
            foreach (var (userinfo, model) in infos)
                DescribedAndSerialized(userinfo, model).WriteTo(file);


            static Type getFormatType(string name) =>
                typeof(_3ds_max).Assembly.GetType($"{typeof(_3ds_max).Namespace}.{name}").ThrowIfNull();
            static Type getRendererType(Type type)
            {
                while (true)
                {
                    type = type.BaseType.ThrowIfNull();
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(NativeFileFormatMetadata<>))
                        break;
                }

                return type.GenericTypeArguments[0];
            }

            static async Task<OperationResult<(InputTurboSquidModelInfoRequest.Response.ResponseModelInfo?, _3DModel)[]>> requestInfo(INodeGui nodeGui, IReadOnlyCollection<_3DModel> models)
            {
                var enumdict = Assembly.GetAssembly(typeof(File)).ThrowIfNull().GetTypes()
                    .Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(NativeFileFormatMetadata)))
                    .Select(t => KeyValuePair.Create(t.Name, Enum.GetNames(getRendererType(t)).ToImmutableArray()))
                    .ToImmutableDictionary();

                var infos = models.Select(model =>
                {
                    var format = FileFormat_(model);
                    if (!format.IsNative())
                        return new InputTurboSquidModelInfoRequest.ModelInfo(model.Name, format.ToString(), null);

                    var renderertype = getRendererType(getFormatType(format.ToString()));
                    return new InputTurboSquidModelInfoRequest.ModelInfo(model.Name, format.ToString(), Enum.GetNames(renderertype).ToImmutableArray());
                }).ToImmutableArray();

                return await
                    from response in nodeGui.Request<InputTurboSquidModelInfoRequest.Response>(new InputTurboSquidModelInfoRequest(infos), default)
                    select response.Infos.Zip(models).ToArray();
            }
            static DocumentSyntax DescribedAndSerialized(InputTurboSquidModelInfoRequest.Response.ResponseModelInfo? userinfo, _3DModel _3DModel)
            {
                var metadata = new DocumentSyntax();
                var table = new TableSyntax(_3DModel.Name);
                var fileFormat = FileFormat_(_3DModel);
                table.Items.Add(PascalToSnakeCase(nameof(TurboSquid3DModelMetadata.FileFormat)), fileFormat.ToString_());
                if (userinfo is not null && fileFormat.IsNative())
                {
                    var type = getFormatType(FileFormat_(_3DModel).ToString());
                    var renderer = Enum.Parse(getRendererType(type), userinfo.Renderer);
                    var instance = (NativeFileFormatMetadata) Activator.CreateInstance(type, [userinfo.FormatVersion, renderer, userinfo.RendererVersion]).ThrowIfNull();

                    table.Items.Add(instance);
                }
                table.Items.Add(PascalToSnakeCase(nameof(TurboSquid3DModelMetadata.IsNative)), userinfo?.IsNative ?? false);
                metadata.Tables.Add(table);
                metadata.AddTrailingTriviaNewLine();

                return metadata;
            }
            static FileFormat FileFormat_(_3DModel model)
            {
                try { return _3DModelComponents.FileFormat_.ToEnum(model); }
                catch (Exception ex)
                { throw new InvalidDataException($"{nameof(TurboSquid3DModelMetadata.FileFormat)} of {nameof(_3DModel)} ({model.Name}) can't be deduced.", ex); }
            }
        }

        internal IEnumerable<TurboSquid3DModelMetadata> Read()
        {
            var toml = Toml.Parse(System.IO.File.ReadAllText(Path));
            var modelsMetadata = int.TryParse(toml.Tables.FirstOrDefault()?.Name?.ToString(), out int _) ?
                ReadPubished3DProductMetadata(toml.Tables) :
                ReadUnpubished3DProductMetadata(toml.Tables);
            return Validated(modelsMetadata);


            IEnumerable<TurboSquid3DModelMetadata> ReadPubished3DProductMetadata(SyntaxList<TableSyntaxBase> tables)
            {
                var productMeta = tables.First();
                _3DProduct.ID = int.Parse(productMeta.Name!.ToString()!);
                _3DProduct.Metadata.Category = productMeta.Items.First() is KeyValueSyntax category ?
                    new _3DProduct.Metadata_.Category_(category.Key!.ToString(), int.Parse(category.Value!.ToString())) :
                    throw new MissingFieldException(nameof(_3DProduct.Metadata.Category));
                if (productMeta.Items.LastOrDefault(_ => _.Value?.ToString() != _3DProduct.Metadata.Category.ID.ToString()) is KeyValueSyntax subcategory)
                    _3DProduct.Metadata.SubCategory = new _3DProduct.Metadata_.Category_(category.Key!.ToString(), int.Parse(category.Value!.ToString()));
                return ReadUnpubished3DProductMetadata(tables.Skip(1));
            }
            IEnumerable<TurboSquid3DModelMetadata> ReadUnpubished3DProductMetadata(IEnumerable<TableSyntaxBase> tables)
                => tables.Select(TurboSquid3DModelMetadata.Read);
        }

        IEnumerable<TurboSquid3DModelMetadata> Validated(IEnumerable<TurboSquid3DModelMetadata> modelsMetadata)
        {
            Exception? exception;
            if (modelsMetadata.Count() == _3DProduct._3DModels.Count)
                if (modelsMetadata.Count(_ => _.IsNative) is 1)
                    return modelsMetadata;
                else exception = new InvalidDataException($"Metadata file must mark one {nameof(_3DModel)} as native.");
            else if (modelsMetadata.Count() is 0 && _3DProduct._3DModels.Count is 1)
                return _3DProduct._3DModels.Select(_ => new TurboSquid3DModelMetadata(_));
            else exception = new InvalidDataException($"Metadata file doesn't describe every model of {nameof(_3DProduct)} ({Path}).");
            System.IO.File.Delete(Path);
            throw exception;
        }

        internal void Write(TurboSquid3DProduct _3DProduct)
        {
            using var _file = System.IO.File.OpenWrite(Path);
            using var file = new StreamWriter(_file);

            if (_3DProduct.ID is not 0)
                Write_(_3DProduct);
            foreach (var model in _3DProduct._3DModels)
                Write(model);


            void Write_(TurboSquid3DProduct _3DProduct)
            {
                var document = new DocumentSyntax();
                document.Tables.Add(_3DProduct);
                document.AddTrailingTriviaNewLine();
                document.WriteTo(file);
            }

            void Write(_3DModel<TurboSquid3DModelMetadata> model)
            {
                var document = new DocumentSyntax();
                document.Tables.Add(model.Metadata);
                document.AddTrailingTriviaNewLine();
                document.WriteTo(file);
            }
        }
    }
}
