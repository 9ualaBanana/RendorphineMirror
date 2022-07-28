namespace Node.Tasks.Models;

public class UserTaskInputInfo : ITaskInputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.User;

    [LocalFile] public readonly string Path;

    public UserTaskInputInfo(string path)
    {
        Path = path;
    }

    public ValueTask<string> Download(ReceivedTask task, HttpClient httpClient, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask Upload()
    {
        throw new NotImplementedException();
    }
}
public class UserTaskOutputInfo : ITaskOutputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.User;

    [LocalDirectory] public readonly string Directory;
    public readonly string FileName;

    public UserTaskOutputInfo(string directory, string fileName)
    {
        Directory = directory;
        FileName = fileName;
    }

    public ValueTask Upload(ReceivedTask task, string file)
    {
        throw new NotImplementedException();
    }
}
