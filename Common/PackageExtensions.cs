using Common;

namespace System.IO.Packaging;

public static class PackageExtensions
{
    /// <inheritdoc cref="CreatePart(Package, Uri, CompressionOption)"/>
    public static PackagePart CreatePart(this Package package, Uri partUri) =>
        package.CreatePart(partUri, CompressionOption.NotCompressed);

    /// <exception cref="ArgumentException">Uri must have an extension specified in its name.</exception>
    public static PackagePart CreatePart(this Package package, Uri partUri, CompressionOption compressionOption)
    {
        if (!Path.HasExtension(partUri.ToString())) throw new ArgumentException(
            "Uri must have an extension specified in its name.", nameof(partUri));

        return package.CreatePart(partUri, MimeTypes.GetMimeType(partUri.ToString()), compressionOption);
    }
}
