using Newtonsoft.Json;

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

[JsonConverter(typeof(WatchingTaskInputJConverter))]
public interface IWatchingTaskOutputInfo : IWatchingTaskInputOutputInfo
{
    WatchingTaskOutputType Type { get; }

    ITaskOutputInfo CreateOutput(WatchingTask task, string file);
}
[JsonConverter(typeof(WatchingTaskOutputJConverter))]
public interface IMPlusWatchingTaskOutputInfo : IWatchingTaskOutputInfo
{
    ITaskOutputInfo CreateOutput(WatchingTask task, MPlusNewItem item, string file);
}