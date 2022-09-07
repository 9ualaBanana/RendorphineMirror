namespace Node.Tasks.Watching;

public class LocalWatchingTaskOutputInfo : IWatchingTaskOutputInfo
{
    public TaskInputOutputType Type => TaskInputOutputType.MPlus;

    public ITaskOutputInfo CreateOutput(string file) => new TorrentTaskOutputInfo();
}
