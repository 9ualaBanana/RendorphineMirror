namespace NodeCommon.Tasks.Watching;

public class OtherUserWatchingTaskInputInfo : IWatchingTaskInputInfo
{
    public WatchingTaskInputType Type => WatchingTaskInputType.OtherNode;

    public readonly string NodeId, Directory;
    [Default(0)] public long LastCheck;

    public OtherUserWatchingTaskInputInfo(string nodeid, string directory)
    {
        NodeId = nodeid;
        Directory = directory;
    }
}
