using System.Collections;

namespace Node.Tasks.Models;

[JsonObject]
public interface IReadOnlyTaskFileListList : IEnumerable<ReadOnlyTaskFileList>
{
    string Directory { get; }
}
[JsonObject]
public class TaskFileListList : IReadOnlyTaskFileListList
{
    [JsonIgnore] public ReadOnlyTaskFileList? InputFiles;

    [JsonProperty] readonly List<ReadOnlyTaskFileList> Lists = new();
    [JsonProperty] public string Directory { get; }

    public TaskFileListList(string directory) => Directory = directory;

    public void Add(ReadOnlyTaskFileList list) => Lists.Add(list);
    public TaskFileList New()
    {
        int index = 0;

        string dir;
        do dir = Path.Combine(Directory, (++index).ToString());
        while (System.IO.Directory.Exists(dir));
        System.IO.Directory.CreateDirectory(dir);

        var list = new TaskFileList(dir) { InputFiles = InputFiles };
        Lists.Add(list);

        return list;
    }


    public IEnumerator<ReadOnlyTaskFileList> GetEnumerator() => Lists.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void AddFromLocalPathFileSeparated(string path)
    {
        var files = FileWithFormat.FromLocalPath(path);
        foreach (var file in files)
            New().Add(file);
    }
}