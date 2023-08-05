using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Node.Tasks.Models;

[JsonObject]
public class ReadOnlyTaskFileList : IEnumerable<FileWithFormat>
{
    // TODO:: TEMPORARY AND WILL BE REFACTORED
    public JToken? OutputJson;

    [JsonIgnore] public IEnumerable<string> Paths => Files.Select(f => f.Path);
    [JsonIgnore] public int Count => Files.Count;
    [JsonProperty] protected readonly HashSet<FileWithFormat> Files;

    public ReadOnlyTaskFileList(IEnumerable<FileWithFormat> files) => Files = files.ToHashSet();

    public FileWithFormat First(FileFormat format) => this.First(f => f.Format == format);
    public FileWithFormat? TryFirst(FileFormat format) => this.FirstOrDefault(f => f.Format == format);
    public FileWithFormat Single(FileFormat format) => this.Single(f => f.Format == format);
    public FileWithFormat? TrySingle(FileFormat format) => this.SingleOrDefault(f => f.Format == format);

    public IEnumerator<FileWithFormat> GetEnumerator() => Files.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
[JsonObject]
public class TaskFileList : ReadOnlyTaskFileList
{
    [JsonIgnore] public ReadOnlyTaskFileList? InputFiles;
    [JsonProperty] public string Directory { get; }

    public TaskFileList(string directory) : base(Enumerable.Empty<FileWithFormat>()) => Directory = directory;

    public void Add(FileWithFormat file) => Files.Add(file);
    public FileWithFormat New(FileFormat format, string? filename = null)
    {
        if (filename is not null && Path.GetExtension(filename) == string.Empty)
            filename += format.AsExtension();

        // use input file name if there is only one input file
        filename ??= ((InputFiles is { Count: 1 } ? Path.GetFileNameWithoutExtension(InputFiles.Single().Path) : "file") + format.AsExtension());
        filename = Path.Combine(Directory, Path.GetFileName(filename));

        var file = new FileWithFormat(format, filename);
        Add(file);
        return file;
    }

    public void Clear() => Files.Clear();
}

public static class TaskFileListExtensions
{
    public static void AddFromLocalPath(this TaskFileList files, string path)
    {
        foreach (var file in FileWithFormat.FromLocalPath(path))
            files.Add(file);
    }

    /// <summary> Ensure files exist </summary>
    public static void ValidateFileList([NotNull] this ReadOnlyTaskFileList? files, string type)
    {
        if (files is null)
            throw new Exception($"Task {type} file list was null or empty");

        foreach (var file in files)
            if (!File.Exists(file.Path))
                throw new Exception($"Task {type} file {file} does not exists");
    }
}