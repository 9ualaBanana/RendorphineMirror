namespace Node.Tasks.Exec;

public static class TaskList
{
    public static readonly ImmutableArray<PluginType> Types = Enum.GetValues<PluginType>().ToImmutableArray();
    public static ImmutableArray<IPluginAction> Actions;

    static TaskList()
    {
        Actions = new[]
        {
            FFMpegTasks.GetTasks(),
        }.SelectMany(x => x).ToImmutableArray();
    }


    public static IEnumerable<IPluginAction> Get(PluginType type) => Actions.Where(x => x.Type == type);
    public static IPluginAction Get(TaskInfo task) => TryGet(task.TaskType) ?? throw new Exception($"Got an unknown task type: {task.TaskType}");
    public static IPluginAction? TryGet(string name) => Actions.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    public static IPluginAction? TryGet(PluginType type, string name) => Actions.FirstOrDefault(x => x.Type == type && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
