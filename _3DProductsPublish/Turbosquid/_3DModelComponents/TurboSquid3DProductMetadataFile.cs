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

            foreach (var _3DModel in _3DProduct._3DModels)
                DescribedAndSerialized(_3DModel).WriteTo(file);


            static DocumentSyntax DescribedAndSerialized(_3DModel _3DModel)
            {
                var metadata = new DocumentSyntax();
                var table = new TableSyntax(_3DModel.Name);
                table.Items.Add(PascalToSnakeCase(nameof(TurboSquid3DModelMetadata.FileFormat)), FileFormat());
                table.Items.Add(PascalToSnakeCase(nameof(TurboSquid3DModelMetadata.FormatVersion)), "formatversion");
                table.Items.Add(PascalToSnakeCase(nameof(TurboSquid3DModelMetadata.IsNative)), "isnative");
                table.Items.Add(PascalToSnakeCase(nameof(TurboSquid3DModelMetadata.Renderer)), "renderer");
                table.Items.Add(PascalToSnakeCase(nameof(TurboSquid3DModelMetadata.RendererVersion)), "rendererversion");
                metadata.Tables.Add(table);
                metadata.AddTrailingTriviaNewLine();

                return metadata;


                string FileFormat()
                {
                    foreach (var file in _3DModel.Files)
                        if (DeduceFromExtension(file) is string fileFormat)
                            return fileFormat;
                    
                    throw new InvalidDataException($"{nameof(TurboSquid3DModelMetadata.FileFormat)} of {nameof(_3DModel)} ({_3DModel.Name}) can't be deduced.");


                    string? DeduceFromExtension(string path)
                        => System.IO.Path.GetExtension(path).ToLowerInvariant() switch
                        {
                            ".blend" => "blender",
                            ".c4d" => "cinema_4d",
                            ".max" => "3ds_max",
                            ".dwg" => "autocad_drawing",
                            ".lwo" => "lightwave",
                            ".fbx" => "fbx",
                            ".ma" or ".mb" => "maya",
                            ".hrc" or ".scn" => "softimage",
                            ".rfa" or ".rvt" => "revit_family",
                            ".obj" or ".mtl" => "obj",
                            _ => null
                        };
                }
            }
        }

        internal IEnumerable<TurboSquid3DModelMetadata> Read()
        {
            var modelsMetadata = Toml.Parse(System.IO.File.ReadAllText(Path))
                .Tables
                .Select(TurboSquid3DModelMetadata.Read);

            if (modelsMetadata.Count() == _3DProduct._3DModels.Count())
                if (modelsMetadata.Count(_ => _.IsNative) is 1)
                    return modelsMetadata;
                else throw new InvalidDataException($"Metadata file can mark only one {nameof(_3DModel)} as native.");
            else throw new InvalidDataException($"Metadata file doesn't describe every model of {nameof(_3DProduct)} ({Path}).");
        }
    }
}
