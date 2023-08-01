using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Node.Tasks.Models;

[JsonObject]
public class ReadOnlyTaskFileList : IEnumerable<FileWithFormat>
{
    [Obsolete("DELETE")]
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
    public FileWithFormat New(FileFormat format, string? filename = null) =>
        NewFile(Directory, format, filename)
            .With(Add);

    public void Clear() => Files.Clear();


    public static FileWithFormat NewFile(string directory, FileFormat format, string? filename = null)
    {
        if (filename is not null && Path.GetExtension(filename) == string.Empty)
            filename += format.AsExtension();

        // use input file name if there is only one input file
        filename ??= ("file" + format.AsExtension());
        filename = Path.Combine(directory, Path.GetFileName(filename));

        return new FileWithFormat(format, filename);
    }
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