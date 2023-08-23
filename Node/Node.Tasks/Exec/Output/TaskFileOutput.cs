using System.Collections;

namespace Node.Tasks.Exec.Output;

public class TaskFileOutput : IConvertibleToMultiInput, IReadOnlyTaskFileListList
{
    public int Count => Files.Count;
    public string Directory => Files.Directory;

    public TaskFileListList Files { get; }

    public TaskFileOutput(string directory) : this(new TaskFileListList(directory)) { }
    public TaskFileOutput(TaskFileListList files) => Files = files;

    public IReadOnlyList<object> ConvertToInput(int index, TaskAction action)
    {
        var dir = Path.Combine(Files.Directory, $"next_{index}_{action}");
        return Files.Select(f => new TaskFileInput(f, dir)).ToArray();
    }

    public IEnumerator<ReadOnlyTaskFileList> GetEnumerator() => Files.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Files.GetEnumerator();
}
