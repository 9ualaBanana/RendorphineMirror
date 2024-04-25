using _3DProductsPublish._3DProductDS;

namespace _3DProductsPublish.CGTrader;

public partial class CGTrader
{
    public record class _3DModelMetadata
    {
        public string Name { get; init; } = default!;
        public int FileFormat { get; init; } = default!;
        public double FormatVersion { get; init; } = 1.0;
        public bool IsNative { get; init; } = false;
        public string? Renderer { get; init; } = default;
        public double? RendererVersion { get; init; } = default;

        public _3DModelMetadata() { }

        public _3DModelMetadata(_3DModel _3DModel)
        {
            Name = _3DModel.Name();
            var fileFormat = FileFormat_.ToEnum(_3DModel);
            FileFormat = (int)fileFormat;
            IsNative = FileFormat_.IsNative(fileFormat);
        }
    }
}

enum FileFormat
{
    unity = 83,
    other = 56
}

static class FileFormat_
{
    internal static bool IsNative(this FileFormat fileFormat)
        => fileFormat is
        FileFormat.unity;

    internal static string ToString_(this FileFormat fileFormat)
        => fileFormat.ToString().TrimStart('_');

    internal static FileFormat ToEnum(string path) =>
        System.IO.Path.GetFileName(path).ToLowerInvariant() is string nameWextension ?
            Dictionary.TryGetValue(System.IO.Path.GetExtension(nameWextension).TrimStart('.'), out FileFormat fileFormat) ? fileFormat
            : System.IO.Path.GetFileNameWithoutExtension(nameWextension) is string nameWOextension
            && Dictionary.TryGetValue(nameWOextension[(nameWOextension.LastIndexOf('_') + 1)..], out fileFormat) ? fileFormat

            : FileFormat.other
        : throw new Exception($"Failed to turn {nameof(path)} into {nameof(nameWextension)}.");
    static ImmutableDictionary<string, FileFormat> Dictionary { get; }
        = new Dictionary<string, FileFormat>
        {
            ["unitypackage"] = FileFormat.unity,
        }.ToImmutableDictionary();
}
