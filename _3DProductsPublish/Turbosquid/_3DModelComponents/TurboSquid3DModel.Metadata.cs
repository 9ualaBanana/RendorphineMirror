using _3DProductsPublish._3DProductDS;

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

    public TurboSquid3DModelMetadata() { }

    public TurboSquid3DModelMetadata(_3DModel _3DModel)
    {
        Name = _3DModel.Name();
        var fileFormat = FileFormat_.ToEnum(_3DModel);
        FileFormat = fileFormat.ToString_();
        IsNative = FileFormat_.IsNative(fileFormat);
    }
}
