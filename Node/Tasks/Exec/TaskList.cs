namespace Node.Tasks.Exec;

public static class TaskList
{
    static readonly List<IPluginAction> Actions = new();


    public static void Add(params IEnumerable<IPluginAction>[] actions) => Add(actions.SelectMany(x => x));
    public static void Add(params IPluginAction[] actions) => Add(actions.AsEnumerable());
    public static void Add(IEnumerable<IPluginAction> actions)
    {
        Actions.AddRange(actions);
        NodeGlobalState.Instance.TaskDefinitions.Value = serializeActions();


        static TasksFullDescriber serializeActions()
        {
            return new TasksFullDescriber(
                Actions.Select(serializeaction).ToImmutableArray(),
                serialize(TaskModels.Inputs),
                serialize(TaskModels.Outputs),
                serialize(TaskModels.WatchingInputs),
                serialize(TaskModels.WatchingOutputs)
            );


            static TaskActionDescriber serializeaction(IPluginAction action) => new TaskActionDescriber(action.Type, action.Name.ToString(), new ObjectDescriber(action.DataType));
            static ImmutableArray<TaskInputOutputDescriber> serialize<T>(ImmutableDictionary<T, Type> dict) where T : struct, Enum =>
                dict.Select(x => new TaskInputOutputDescriber(x.Key.ToString(), new ObjectDescriber(x.Value))).ToImmutableArray();
        }
    }


    public static IPluginAction GetFirstAction(this ReceivedTask task) => task.Info.GetFirstAction();
    public static IPluginAction GetFirstAction(this TaskInfo task) => GetAction(task.FirstTaskType);
    public static IPluginAction GetAction(string action) => TryGet(action) ?? throw new Exception($"Got an unknown task type: {action}");

    public static IEnumerable<IPluginAction> Get(PluginType type) => Actions.Where(x => x.Type == type);
    public static IPluginAction Get(string name) => Actions.First(x => x.Name.ToString().Equals(name, StringComparison.OrdinalIgnoreCase));
    public static IPluginAction? TryGet(string name) => Actions.FirstOrDefault(x => x.Name.ToString().Equals(name, StringComparison.OrdinalIgnoreCase));
    public static IPluginAction? TryGet(PluginType type, string name) => Actions.FirstOrDefault(x => x.Type == type && x.Name.ToString().Equals(name, StringComparison.OrdinalIgnoreCase));
}
