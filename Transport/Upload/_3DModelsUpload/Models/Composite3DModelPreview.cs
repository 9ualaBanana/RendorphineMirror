namespace Transport.Upload._3DModelsUpload.Models;

internal static class Composite3DModelPreview
{
    internal static IEnumerable<string> _ValidateExtensions(IEnumerable<string>? pathsOrExtensions)
    {
        if (pathsOrExtensions is null) return Enumerable.Empty<string>();

        foreach (var pathOrExtension in pathsOrExtensions)
            _ValidateExtension(pathOrExtension);

        return pathsOrExtensions;
    }

    internal static string _ValidateExtension(string pathOrExtension)
    {
        if (!_HasValidExtension(pathOrExtension))
            throw new ArgumentOutOfRangeException(
                nameof(pathOrExtension),
                pathOrExtension,
                "The file extension of one of the files is not allowed for previews.");
        else return pathOrExtension;
    }

    internal static bool _HasValidExtension(string pathOrExtension) =>
        _allowedExtensions.Contains(Path.GetExtension(pathOrExtension));

    static string[] _allowedExtensions = { ".jpeg", ".jpg", ".png" };
}
