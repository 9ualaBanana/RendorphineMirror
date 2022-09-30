using Newtonsoft.Json.Linq;

namespace Common.Tasks;

public class WatchingTaskInfo
{
    public readonly string Id;
    public readonly string? Version;
    public readonly string TaskAction;
    public readonly JObject Source, Output, TaskData;
    public readonly TaskPolicy Policy;
    public readonly bool ExecuteLocally;


#pragma warning disable CS8618 // field are not assigned
    public WatchingTaskInfo() { }
#pragma warning restore
}
