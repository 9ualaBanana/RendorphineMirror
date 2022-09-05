using Newtonsoft.Json.Linq;

namespace Common.Tasks;

public record TaskFullState(TaskState State, double Progress, JObject Output, TaskServer? Server = null);
public record TaskServer(string Host, string Userid, string Nickname);


public record DbTaskFullState(string Id, string UserId, ulong Registered, string OriginGuid, TaskPolicy LaunchPolicy,
    TaskState State, double Progress, TaskObject @Object, JObject Input, JObject Output, JObject Data, TaskServer? Server = null)
    : TaskFullState(State, Progress, Output, Server)
{
    public ReceivedTask ToReceived() => new ReceivedTask(Id, new TaskInfo(@Object, Input, Output, Data, LaunchPolicy, OriginGuid), false) { Progress = Progress };
}