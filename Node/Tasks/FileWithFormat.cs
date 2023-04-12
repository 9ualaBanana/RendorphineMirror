namespace Node.Tasks;

public record FileWithFormat(FileFormat Format, string Path)
{
    public static FileWithFormat FromFile(string path) => new(FileFormatExtensions.FromFilename(path), path);
}