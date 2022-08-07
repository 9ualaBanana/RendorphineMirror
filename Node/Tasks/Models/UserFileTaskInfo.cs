namespace Node.Tasks.Models;

public class UserTaskInputInfo : ITaskInputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.User;

    [LocalFile] public readonly string Path;

    public UserTaskInputInfo(string path)
    {
        Path = path;
    }

    public ValueTask<string> Download(ReceivedTask task, CancellationToken cancellationToken)
    {
        if (task.ExecuteLocally) return Path.AsVTask();

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
        if (task.ExecuteLocally)
        {
            File.Move(file, Path.Combine(Directory, FileName), true);
            return ValueTask.CompletedTask;
        }

        throw new NotImplementedException();
    }
}
