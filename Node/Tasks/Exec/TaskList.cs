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


            static TaskActionDescriber serializeaction(IPluginAction action) => new TaskActionDescriber(action.Type, action.Name, (ObjectDescriber) FieldDescriber.Create(action.DataType));
            static ImmutableArray<TaskInputOutputDescriber> serialize<T>(ImmutableDictionary<T, Type> dict) where T : struct, Enum =>
                dict.Select(x => new TaskInputOutputDescriber(x.Key.ToString(), (ObjectDescriber) FieldDescriber.Create(x.Value))).ToImmutableArray();
        }
    }


    public static IEnumerable<IPluginAction> Get(PluginType type) => Actions.Where(x => x.Type == type);
    public static IPluginAction GetAction(this ReceivedTask task) => task.Info.GetAction();
    public static IPluginAction GetAction(this TaskInfo task) => TryGet(task.TaskType) ?? throw new Exception($"Got an unknown task type: {task.TaskType}");
    public static IPluginAction Get(string name) => Actions.First(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    public static IPluginAction? TryGet(string name) => Actions.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    public static IPluginAction? TryGet(PluginType type, string name) => Actions.FirstOrDefault(x => x.Type == type && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
