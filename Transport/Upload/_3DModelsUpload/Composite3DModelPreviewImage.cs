namespace Transport.Upload._3DModelsUpload;

internal static class Composite3DModelPreviewImage
{
    internal static IEnumerable<string> _EnumerateIn(string _3DModelDirectory) =>
        Directory.EnumerateFiles(_3DModelDirectory).Where(_HasValidExtension);

    static bool _HasValidExtension(string pathOrExtension) =>
        _allowedExtensions.Contains(Path.GetExtension(pathOrExtension));

    readonly static string[] _allowedExtensions = { ".jpeg", ".jpg", ".png" };
}
