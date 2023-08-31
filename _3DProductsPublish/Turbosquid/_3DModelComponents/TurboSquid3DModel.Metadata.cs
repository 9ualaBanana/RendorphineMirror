using _3DProductsPublish._3DProductDS;
using Tomlyn;
using Tomlyn.Syntax;

namespace _3DProductsPublish.Turbosquid._3DModelComponents;

public partial record TurboSquid3DModelMetadata : I3DModelMetadata
{
    public string Name { get; private init; } = default!;
    public string FileFormat { get; private init; } = default!;
    public double FormatVersion { get; private init; } = 1.0;
    public bool IsNative { get; private init; } = false;
    public string? Renderer { get; private init; } = default;
    public double? RendererVersion { get; private init; } = default;

    public static TurboSquid3DModelMetadata Read(TableSyntaxBase table)
    {
        var modelName = table.Name?.ToString();
        ArgumentException.ThrowIfNullOrEmpty(modelName, nameof(modelName));

        return Toml.ToModel<TurboSquid3DModelMetadata>(table.Items.ToString()) with { Name = modelName };
    }
    public TurboSquid3DModelMetadata() { }
}
