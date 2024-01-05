namespace Node.Tasks.Models;

public record FileWithFormat
{
    public FileFormat Format { get; private set; }
    public string Path { get; private set; }

    public FileWithFormat(FileFormat format, string path)
    {
        Format = format;
        Path = path;
    }

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

    public void MoveTo(string destination, string? name = default)
    {
        name = System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(name ?? Path), System.IO.Path.GetExtension(Path));
        var newPath = System.IO.Path.Combine(Directory.CreateDirectory(destination).FullName, name);
        File.Move(Path, newPath);
        Path = newPath;
    }

    public static implicit operator string(FileWithFormat file) => file.Path;
}