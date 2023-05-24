namespace Node.Tasks.Models;

public record FileWithFormat(FileFormat Format, string Path)
{
    public static FileWithFormat FromFile(string path) => new(FileFormatExtensions.FromFilename(path), path);
    public static IEnumerable<FileWithFormat> FromLocalPath(string path)
    {
        if (Directory.Exists(path))
            return fromDir(path);

        if (File.Exists(path))
            return new[] { fromFile(path) };

        throw new Exception($"Could not find file or directory {path}");


        FileWithFormat fromFile(string file) => FileWithFormat.FromFile(file);

        IEnumerable<FileWithFormat> fromDir(string dir) =>
            Directory.GetDirectories(dir).SelectMany(fromDir)
            .Concat(Directory.GetFiles(dir).Select(fromFile));
    }
}