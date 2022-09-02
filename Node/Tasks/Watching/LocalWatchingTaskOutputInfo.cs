namespace Node.Tasks.Watching;

public class LocalWatchingTaskOutputInfo : IWatchingTaskOutputInfo
{
    public TaskOutputType Type => TaskOutputType.MPlus;
    [LocalDirectory] public readonly string Directory;

    public LocalWatchingTaskOutputInfo(string directory) => Directory = directory;

    public ITaskOutputInfo CreateOutput(string file) => new UserTaskOutputInfo(Directory, file);
}
