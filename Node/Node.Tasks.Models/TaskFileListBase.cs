namespace Node.Tasks.Models;

[JsonObject]
public abstract class TaskFileListBase
{
    [JsonProperty] protected readonly string Directory;
    [JsonProperty] protected readonly HashSet<FileWithFormat> Files = new();

    protected TaskFileListBase(string directory) => Directory = directory;

    protected void Add(FileWithFormat file) => Files.Add(file);
    protected FileWithFormat New(FileFormat format, string? filename = null)
    {
        if (filename is not null && Path.GetExtension(filename) == string.Empty)
            filename += format.AsExtension();

        filename ??= ("file" + format.AsExtension());
        filename = Path.Combine(Directory, Path.GetFileName(filename));

        var file = new FileWithFormat(format, filename);
        Add(file);
        return file;
    }

    protected void Clear() => Files.Clear();
}
