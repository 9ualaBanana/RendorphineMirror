namespace NodeCommon.Tasks;

public abstract record TaskBase(string Id, TaskInfo Info) : IMPlusTask
{
    public string? HostShard { get; set; }

    // 0-1
    public double Progress { get; set; } = 0;
    public TaskState State { get; set; } = TaskState.Queued;
    public TaskTimes Times { get; set; } = new();

    [JsonIgnore] public string FirstAction => Info.FirstTaskType;
    [JsonIgnore] public IEnumerable<string> Actions => (Info.Next ?? ImmutableArray<Newtonsoft.Json.Linq.JObject>.Empty).Select(TaskInfo.GetTaskType).Prepend(FirstAction);
    [JsonIgnore] public IEnumerable<ITaskInputInfo> Inputs => (Info.Inputs ?? new[] { Info.Input.ThrowIfNull() });
    [JsonIgnore] public ITaskOutputInfo Output => Info.Output;

    public void SetStateTime(TaskState state)
    {
        var time = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        Times ??= new();
        Times = state switch
        {
            TaskState.Queued => Times,
            TaskState.Input => Times with { Input = Times.Input ?? time },
            TaskState.Active => Times with { Active = Times.Active ?? time },
            TaskState.Output => Times with { Output = Times.Output ?? time },
            TaskState.Validation => Times with { Output = Times.Validation ?? time },
            TaskState.Finished => Times with { Finished = Times.Finished ?? time },
            TaskState.Failed => Times with { Failed = Times.Failed ?? time },
            TaskState.Canceled => Times with { Canceled = Times.Canceled ?? time },
            _ => throw new InvalidOperationException(),
        };
    }

    public static string GenerateLocalId() => "local_" + Guid.NewGuid();

    public bool Equals(IRegisteredTask? other) => Id == other?.Id;
}
