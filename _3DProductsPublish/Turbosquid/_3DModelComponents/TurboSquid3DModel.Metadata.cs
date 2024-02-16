using _3DProductsPublish._3DProductDS;
using Tomlyn;
using Tomlyn.Syntax;
using static Tomlyn.Helpers.TomlNamingHelper;

namespace _3DProductsPublish.Turbosquid._3DModelComponents;

// Shall be different for Native and Non-Native file formats.
public partial record TurboSquid3DModelMetadata : _3DModel.IMetadata
{
    public long? ID { get; internal set; } = default!;
    public string Name { get; init; } = default!;
    public string FileFormat { get; init; } = default!;
    public double FormatVersion { get; init; } = 1.0;
    public bool IsNative { get; init; } = false;
    public string Renderer { get; init; } = "other"!;
    public double? RendererVersion { get; init; } = default;

    public static TurboSquid3DModelMetadata Read(TableSyntaxBase table)
    {
        var modelName = table.Name?.ToString();
        ArgumentException.ThrowIfNullOrEmpty(modelName, nameof(modelName));

        return Toml.ToModel<TurboSquid3DModelMetadata>(table.Items.ToString()) with { Name = modelName };
    }
    public TurboSquid3DModelMetadata() { }

    public TurboSquid3DModelMetadata(_3DModel _3DModel)
    {
        Name = _3DModel.Name;
        var fileFormat = FileFormat_.ToEnum(_3DModel);
        FileFormat = fileFormat.ToString_();
        IsNative = FileFormat_.IsNative(fileFormat);
    }


    public static implicit operator TableSyntax(TurboSquid3DModelMetadata metadata)
    {
        var table = new TableSyntax(metadata.Name);
        if (metadata.ID is long id)
            table.Items.Add(PascalToSnakeCase(nameof(ID)), id);
        table.Items.Add(PascalToSnakeCase(nameof(FileFormat)), metadata.FileFormat);
        table.Items.Add(PascalToSnakeCase(nameof(FormatVersion)), metadata.FormatVersion);
        if (metadata.Renderer is string renderer)
            table.Items.Add(PascalToSnakeCase(nameof(Renderer)), renderer);
        if (metadata.RendererVersion is double rendererVersion)
            table.Items.Add(PascalToSnakeCase(nameof(RendererVersion)), rendererVersion);
        table.Items.Add(PascalToSnakeCase(nameof(IsNative)), metadata.IsNative);
        return table;
    }
}
