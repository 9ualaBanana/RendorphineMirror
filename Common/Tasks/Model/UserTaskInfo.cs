namespace Common.Tasks.Model;

public class UserTaskInputInfo : ITaskInputInfo
{
    public TaskInputType Type => TaskInputType.User;

    [LocalFile] public readonly string Path;

    public UserTaskInputInfo(string path) => Path = path;
}
public class UserTaskOutputInfo : ITaskOutputInfo
{
    public TaskOutputType Type => TaskOutputType.User;

    [LocalDirectory] public readonly string Directory;
    public readonly string FileName;

    public UserTaskOutputInfo(string directory, string fileName)
    {
        Directory = directory;
        FileName = fileName;
    }
}
