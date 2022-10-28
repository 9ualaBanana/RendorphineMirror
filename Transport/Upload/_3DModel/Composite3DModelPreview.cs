namespace Transport.Upload._3DModel;

internal static class Composite3DModelPreview
{
    internal static IEnumerable<string> _ValidatePreviews(IEnumerable<string>? previews)
    {
        if (previews is null) return Enumerable.Empty<string>();

        string extension;
        foreach (var preview in previews)
        {
            extension = Path.GetExtension(preview);
            if (!_HasValidExtension(extension))
                throw new ArgumentOutOfRangeException(
                    nameof(extension),
                    preview,
                    "The file extension of one of the files is not allowed for previews.");
        }
        return previews;
    }

    internal static bool _HasValidExtension(string pathOrExtension) =>
        _allowedExtensions.Contains(pathOrExtension);

    static string[] _allowedExtensions = { ".jpeg", ".jpg", ".png" };
}
