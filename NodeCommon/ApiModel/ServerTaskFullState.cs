namespace NodeCommon.ApiModel;

public record ServerTaskFullState : DbTaskFullState, ILoggable
{
    public TaskServer? Server { get; set; }

    [JsonConstructor]
    public ServerTaskFullState(string id, TaskPolicy launchPolicy, TaskObject @object, ITaskInputInfo input, ITaskOutputInfo output, JObject data)
        : base(id, new TaskInfo(@object, input, output, data, launchPolicy)) { }
}