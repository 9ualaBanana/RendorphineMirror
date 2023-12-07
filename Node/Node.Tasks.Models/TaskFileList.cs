using System.Collections;

namespace Node.Tasks.Models;

public interface IReadOnlyTaskFileList : IEnumerable<FileWithFormat>
{
    int Count { get; }
}
[JsonObject]
public class ReadOnlyTaskFileList : IReadOnlyTaskFileList
{
    [Obsolete("DELETE")]
    // TODO:: TEMPORARY AND WILL BE REFACTORED
    public JToken? OutputJson;

    [JsonIgnore] public IEnumerable<string> Paths => Files.Select(f => f.Path);
    [JsonIgnore] public int Count => Files.Count;
    [JsonProperty] protected readonly HashSet<FileWithFormat> Files;

    public ReadOnlyTaskFileList(IEnumerable<FileWithFormat> files) => Files = files.ToHashSet();

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

    public void Remove(FileWithFormat file) => Files.Remove(file);
    public void Clear() => Files.Clear();


    public static FileWithFormat NewFile(string directory, FileFormat format, string? filename = null)
    {
        var extension = Path.GetExtension(filename ?? "file");
        if (extension.Length == 0) extension = format.AsExtension();

        filename = Directories.RandomNameInDirectory(directory, extension);
        return new FileWithFormat(format, filename);
    }
}
public static class TaskFileListExtensions
{
    public static FileWithFormat First(this IReadOnlyTaskFileList list, FileFormat format) => list.First(f => f.Format == format);
    public static FileWithFormat? TryFirst(this IReadOnlyTaskFileList list, FileFormat format) => list.FirstOrDefault(f => f.Format == format);
    public static FileWithFormat Single(this IReadOnlyTaskFileList list, FileFormat format) => list.Single(f => f.Format == format);
    public static FileWithFormat? TrySingle(this IReadOnlyTaskFileList list, FileFormat format) => list.SingleOrDefault(f => f.Format == format);

    public static bool Contains(this IReadOnlyTaskFileList list, FileFormat format) => list.Any(f => f.Format == format);

    public static void AddFromLocalPath(this TaskFileList files, string path)
    {
        foreach (var file in FileWithFormat.FromLocalPath(path))
            files.Add(file);
    }

    /// <summary> Ensure files exist </summary>
    public static void ValidateFileList([NotNull] this IReadOnlyTaskFileList? files, string type)
    {
        if (files is null)
            throw new Exception($"Task {type} file list was null or empty");

        foreach (var file in files)
            if (!File.Exists(file.Path))
                throw new Exception($"Task {type} file {file} does not exists");
    }
}
