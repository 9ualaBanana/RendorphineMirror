using System.Collections;
using Newtonsoft.Json;

namespace NodeToUI;

[JsonObject]
public interface IReadOnlyTaskFileListList : IEnumerable<IReadOnlyTaskFileList>
{
    string Directory { get; }
}
[JsonObject]
public class TaskFileListList : IReadOnlyTaskFileListList
{
    [JsonIgnore] public IReadOnlyTaskFileList? InputFiles;

    [JsonProperty(nameof(Lists))] readonly List<TaskFileList> Lists = new();
    [JsonProperty(nameof(Directory))] public string Directory { get; }

    public TaskFileListList(string directory) => Directory = directory;

    public void Add(TaskFileList list) => Lists.Add(list);
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


    public IEnumerator<IReadOnlyTaskFileList> GetEnumerator() => Lists.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void AddFromLocalPath(string path)
    {
        var files = TaskFileList.FromLocalPath(path);
        foreach (var file in files)
            New().Add(file);
    }
}