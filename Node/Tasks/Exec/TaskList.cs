namespace Node.Tasks.Exec;

public static class TaskList
{
    public static readonly ImmutableArray<PluginType> Types = Enum.GetValues<PluginType>().ToImmutableArray();
    static readonly ImmutableArray<ITaskExecutor> Executors;
    public static ImmutableArray<IPluginAction> Actions;

    static TaskList()
    {
        Executors = new ITaskExecutor[]
        {
            FFMpegTasks.Instance,
            EsrganTasks.Instance,
        }.ToImmutableArray();

        Actions = Executors.SelectMany(x => x.GetTasks()).ToImmutableArray();
    }


    public static IEnumerable<IPluginAction> Get(PluginType type) => Actions.Where(x => x.Type == type);
    public static IPluginAction GetAction(this ReceivedTask task) => task.Info.GetAction();
    public static IPluginAction GetAction(this TaskInfo task) => TryGet(task.TaskType) ?? throw new Exception($"Got an unknown task type: {task.TaskType}");
    public static IPluginAction Get(string name) => Actions.First(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    public static IPluginAction? TryGet(string name) => Actions.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    public static IPluginAction? TryGet(PluginType type, string name) => Actions.FirstOrDefault(x => x.Type == type && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
