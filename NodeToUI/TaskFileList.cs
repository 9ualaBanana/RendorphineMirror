using System.Collections;
using Newtonsoft.Json;

namespace NodeToUI;

[JsonObject]
public interface IReadOnlyTaskFileList : IEnumerable<FileWithFormat>
{
    [JsonIgnore] int Count { get; }
    [JsonIgnore] IEnumerable<string> Paths { get; }

    public string First(FileFormat format);
    public string? TryFirst(FileFormat format);
    public string Single(FileFormat format);
    public string? TrySingle(FileFormat format);
}
public class TaskFileList : IReadOnlyTaskFileList
{
    [JsonIgnore] public IEnumerable<string> Paths => this.Select(f => f.Path);
    [JsonIgnore] public int Count => Files.Count;
    [JsonIgnore] public IReadOnlyTaskFileList? InputFiles;

    [JsonProperty(nameof(Files))] readonly HashSet<FileWithFormat> Files = new();
    [JsonProperty(nameof(Directory))] readonly string Directory;

    [JsonConstructor]
    public TaskFileList(string directory) => Directory = directory;
    public TaskFileList(string directory, IEnumerable<FileWithFormat> files) : this(directory)
    {
        foreach (var file in files)
            Add(file);
    }


    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("extension")]
    static string? AsExtension(string? extension)
    {
        extension = Path.GetExtension(extension);
        if (extension is null || extension.StartsWith('.'))
            return extension;

        return "." + extension;
    }
    static string? AsExtension(FileFormat format) => AsExtension("." + format.ToString().ToLowerInvariant());

    public void Add(FileWithFormat file) => Files.Add(file);
    public string FSNewFile(FileFormat format, string? filename = null)
    {
        // use input file name if there is only one input file
        filename ??= ((InputFiles is { Count: 1 } ? Path.GetFileNameWithoutExtension(InputFiles.Single().Path) : "file") + AsExtension(format));
        filename = Path.Combine(Directory, Path.GetFileName(filename));

        Files.Add(new FileWithFormat(format, filename));
        return filename;
    }

    public string First(FileFormat format) => Files.First(x => x.Format == format).Path;
    public string? TryFirst(FileFormat format) => Files.FirstOrDefault(x => x.Format == format)?.Path;
    public string Single(FileFormat format) => Files.Single(x => x.Format == format).Path;
    public string? TrySingle(FileFormat format) => Files.SingleOrDefault(x => x.Format == format)?.Path;


    public void AddFromLocalPath(string path)
    {
        if (System.IO.Directory.Exists(path)) addDir(path);
        else addFile(path);


        void addDir(string dir)
        {
            foreach (var dir2 in System.IO.Directory.GetDirectories(dir))
                addDir(dir2);

            foreach (var file in System.IO.Directory.GetFiles(dir))
                addFile(file);
        }
        void addFile(string file) => Add(FileWithFormat.FromFile(file));
    }
    public static TaskFileList FromLocalPath(string path)
    {
        var files = new TaskFileList(System.IO.Path.GetDirectoryName(path).ThrowIfNull());
        files.AddFromLocalPath(path);

        return files;
    }


    public IEnumerator<FileWithFormat> GetEnumerator() => Files.OrderByDescending(x => x.Format).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}