namespace NodeCommon.Tasks.Watching;

public class MPlusWatchingTaskOutputInfo : IWatchingTaskOutputInfo
{
    public WatchingTaskOutputType Type => WatchingTaskOutputType.MPlus;
    public readonly string Directory;

    public MPlusWatchingTaskOutputInfo(string directory) => Directory = directory;

    public ITaskOutputInfo CreateOutput(WatchingTask task, string file) => new MPlusTaskOutputInfo(file, Directory);
}
