using System.Collections;

namespace Node.Tasks.Exec.Input;

[JsonObject]
public class TaskFileInput : IReadOnlyTaskFileList
{
    public int Count => Files.Count;

    public IReadOnlyTaskFileList Files { get; }
    public string ResultDirectory { get; }

    public TaskFileInput(IReadOnlyTaskFileList files, string resultDirectory)
    {
        Files = files;
        ResultDirectory = resultDirectory;
    }

    public IEnumerator<FileWithFormat> GetEnumerator() => Files.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
