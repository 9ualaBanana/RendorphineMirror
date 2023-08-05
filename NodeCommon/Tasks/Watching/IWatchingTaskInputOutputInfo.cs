using Newtonsoft.Json;

namespace NodeCommon.Tasks.Watching;

public interface IWatchingTaskInputOutputInfo { }
[JsonConverter(typeof(WatchingTaskInputJConverter))]
public interface IWatchingTaskInputInfo : IWatchingTaskInputOutputInfo
{
    WatchingTaskInputType Type { get; }
}
public interface IMPlusWatchingTaskInputInfo : IWatchingTaskInputInfo { }

[JsonConverter(typeof(WatchingTaskOutputJConverter))]
public interface IWatchingTaskOutputInfo : IWatchingTaskInputOutputInfo
{
    WatchingTaskOutputType Type { get; }

    ITaskOutputInfo CreateOutput(WatchingTask task, string file);
}
public interface IMPlusWatchingTaskOutputInfo : IWatchingTaskOutputInfo
{
    ITaskOutputInfo CreateOutput(WatchingTask task, MPlusNewItem item, string file);
}