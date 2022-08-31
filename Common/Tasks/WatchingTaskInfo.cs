using Newtonsoft.Json.Linq;

namespace Common.Tasks;

public class WatchingTaskInfo
{
    public readonly string Id;
    public readonly string? Version;
    public readonly string TaskAction;
    public readonly JObject Source, Output, TaskData;
    public readonly TaskPolicy Policy;

    public WatchingTaskInfo(string id, string? version, string taskaction, JObject source, JObject output, JObject taskdata, TaskPolicy policy = TaskPolicy.AllNodes, bool executeLocally = false)
    {
        Id = id;
        Version = version;
        TaskAction = taskaction;
        Source = source;
        Output = output;
        TaskData = taskdata;
        Policy = policy;

        if (executeLocally) Policy = TaskPolicy.OwnNodes;
    }
}
