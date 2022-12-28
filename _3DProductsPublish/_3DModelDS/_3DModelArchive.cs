namespace _3DProductsPublish._3DModelDS;

internal static class _3DModelArchive
{
    internal static IEnumerable<string> EnumerateIn(string directoryPath) =>
        Directory.EnumerateFiles(directoryPath).Where(HasValidExtension);

    internal static bool Exists(string path) => File.Exists(path) && HasValidExtension(path);

    static bool HasValidExtension(string pathOrExtension) =>
        _validExtensions.Contains(Path.GetExtension(pathOrExtension));

    readonly static string[] _validExtensions = { ".zip", ".rar" };
}
