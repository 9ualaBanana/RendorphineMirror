namespace _3DProductsPublish._3DModelDS;

internal static class _3DModelArchive
{
    internal static IEnumerable<string> _EnumerateIn(string directoryPath) =>
        Directory.EnumerateFiles(directoryPath).Where(_HasValidExtension);

    internal static bool Exists(string path) => File.Exists(path) && _HasValidExtension(path);

    static bool _HasValidExtension(string pathOrExtension) =>
        _validExtensions.Contains(Path.GetExtension(pathOrExtension));

    readonly static string[] _validExtensions = { ".zip", ".rar" };
}
