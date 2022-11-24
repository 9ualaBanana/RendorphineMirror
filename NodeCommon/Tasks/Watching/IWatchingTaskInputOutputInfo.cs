namespace NodeCommon.Tasks.Watching;

public interface IWatchingTaskInputOutputInfo { }
public interface IWatchingTaskInputInfo : IWatchingTaskInputOutputInfo
{
    WatchingTaskInputType Type { get; }
}
public interface IMPlusWatchingTaskInputInfo : IWatchingTaskInputInfo
{
    string? SinceIid { get; set; }
}

public interface IWatchingTaskOutputInfo : IWatchingTaskInputOutputInfo
{
    WatchingTaskOutputType Type { get; }

    ITaskOutputInfo CreateOutput(WatchingTask task, string file);
}
public interface IMPlusWatchingTaskOutputInfo : IWatchingTaskOutputInfo
{
    ITaskOutputInfo CreateOutput(WatchingTask task, MPlusNewItem item, string file);
}