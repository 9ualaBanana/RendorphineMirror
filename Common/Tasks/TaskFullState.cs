using Newtonsoft.Json.Linq;

namespace Common.Tasks;

public record TaskFullState(TaskState State, double Progress, JObject Output, TaskServer? Server = null);
public record TaskServer(string Host, string Userid, string Nickname);


public record DbTaskFullState(string Id, string UserId, ulong Registered, TaskPolicy LaunchPolicy, TaskState State, double Progress, JObject Input, JObject Output, JObject Data, TaskServer? Server = null)
    : TaskFullState(State, Progress, Output, Server);