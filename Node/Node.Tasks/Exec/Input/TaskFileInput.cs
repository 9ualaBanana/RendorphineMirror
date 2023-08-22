using System.Collections;

namespace Node.Tasks.Exec.Input;

[JsonObject]
public class TaskFileInput : IEnumerable<FileWithFormat>
{
    public ReadOnlyTaskFileList Files { get; }
    public string ResultDirectory { get; }

    public TaskFileInput(ReadOnlyTaskFileList files, string resultDirectory)
    {
        Files = files;
        ResultDirectory = resultDirectory;
    }

    public IEnumerator<FileWithFormat> GetEnumerator() => Files.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
