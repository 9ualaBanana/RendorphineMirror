namespace Node.Tasks.IO.Output;

public interface ITaskOutputDirectoryProvider
{
    string OutputDirectory { get; }
}
public class TaskOutputDirectoryProvider : ITaskOutputDirectoryProvider
{
    public string OutputDirectory { get; }

    public TaskOutputDirectoryProvider(string outputDirectory) => OutputDirectory = outputDirectory;
}
