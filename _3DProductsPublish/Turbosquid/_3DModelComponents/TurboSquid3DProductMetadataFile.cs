using System.Reflection;
using _3DProductsPublish._3DProductDS;
using NodeToUI;
using NodeToUI.Requests;
using Tomlyn;
using Tomlyn.Syntax;
using static Tomlyn.Helpers.TomlNamingHelper;

namespace _3DProductsPublish.Turbosquid._3DModelComponents;

public partial record TurboSquid3DProductMetadata
{
    internal class File
    {
        internal readonly string Name;
        internal string Path => _path ??= System.IO.Path.Combine(_3DProduct.ContainerPath, Name);
        string? _path;
        internal readonly _3DProduct<TurboSquid3DProductMetadata> _3DProduct;

        internal static File For(_3DProduct<TurboSquid3DProductMetadata> _3DProduct, string name = "turbosquid.meta")
            => new(_3DProduct, name);
        File(_3DProduct<TurboSquid3DProductMetadata> _3DProduct, string name = "turbosquid.meta")
        {
            this._3DProduct = _3DProduct;
            Name = name;
        }

        internal void Populate()
        {
            using var _file = System.IO.File.OpenWrite(Path);
            using var file = new StreamWriter(_file);

            var infos = requestInfo(_3DProduct._3DModels.ToArray()).Result.ThrowIfError();
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

            static async Task<OperationResult<(InputTurboSquidModelInfoRequest.Response.ResponseModelInfo?, _3DModel)[]>> requestInfo(IReadOnlyCollection<_3DModel> models)
            {
                var enumdict = Assembly.GetAssembly(typeof(File)).ThrowIfNull().GetTypes()
                    .Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(NativeFileFormatMetadata)))
                    .Select(t => KeyValuePair.Create(t.Name, Enum.GetNames(getRendererType(t)).ToImmutableArray()))
                    .ToImmutableDictionary();

                var infos = models.Select(f =>
                {
                    var format = FileFormat_(f);
                    if (!format.IsNative())
                        return new InputTurboSquidModelInfoRequest.ModelInfo(f.Name, format.ToString(), null);

                    var renderertype = getRendererType(getFormatType(format.ToString()));
                    return new InputTurboSquidModelInfoRequest.ModelInfo(f.Name, format.ToString(), Enum.GetNames(renderertype).ToImmutableArray());
                }).ToImmutableArray();

                return await
                    from response in NodeGui.Request<InputTurboSquidModelInfoRequest.Response>(new InputTurboSquidModelInfoRequest(infos), default)
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
                    var instance = (NativeFileFormatMetadata) Activator.CreateInstance(type, new object?[] { userinfo.FormatVersion, renderer, userinfo.RendererVersion }).ThrowIfNull();

                    table.Items.Add(instance);
                }

                table.Items.Add(PascalToSnakeCase(nameof(TurboSquid3DModelMetadata.IsNative)), userinfo?.IsNative ?? false);
                metadata.Tables.Add(table);
                metadata.AddTrailingTriviaNewLine();

                return metadata;
            }
            static FileFormat FileFormat_(_3DModel model)
            {
                foreach (var file in model.Files)
                    if (DeduceFromExtension(file) is FileFormat fileFormat)
                        return fileFormat;

                throw new InvalidDataException($"{nameof(TurboSquid3DModelMetadata.FileFormat)} of {nameof(_3DModel)} ({model.Name}) can't be deduced.");


                static FileFormat? DeduceFromExtension(string path)
                    => System.IO.Path.GetExtension(path).ToLowerInvariant() switch
                    {
                        ".blend" => FileFormat.blender,
                        ".c4d" => FileFormat.cinema_4d,
                        ".max" => FileFormat._3ds_max,
                        ".dwg" => FileFormat.autocad_drawing,
                        ".lwo" => FileFormat.lightwave,
                        ".fbx" => FileFormat.fbx,
                        ".ma" or ".mb" => FileFormat.maya,
                        ".hrc" or ".scn" => FileFormat.softimage,
                        ".rfa" or ".rvt" => FileFormat.revit_family,
                        ".obj" or ".mtl" => FileFormat.obj,
                        _ => null
                    };
            }
        }

        internal IEnumerable<TurboSquid3DModelMetadata> Read()
        {
            var modelsMetadata = Toml.Parse(System.IO.File.ReadAllText(Path))
                .Tables
                .Select(TurboSquid3DModelMetadata.Read);
            return Validated(modelsMetadata);
        }

        IEnumerable<TurboSquid3DModelMetadata> Validated(IEnumerable<TurboSquid3DModelMetadata> modelsMetadata)
        {
            Exception? exception;
            if (modelsMetadata.Count() == _3DProduct._3DModels.Count())
                if (modelsMetadata.Count(_ => _.IsNative) is 1)
                    return modelsMetadata;
                else exception = new InvalidDataException($"Metadata file must mark one {nameof(_3DModel)} as native.");
            else exception = new InvalidDataException($"Metadata file doesn't describe every model of {nameof(_3DProduct)} ({Path}).");
            System.IO.File.Delete(Path);
            throw exception;
        }
    }
}
