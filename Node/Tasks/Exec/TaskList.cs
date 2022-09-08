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
            var actions = Actions.Select(serializeaction).ToImmutableArray();
            var inputs = TaskInputOutputInfo.Inputs.Select(k => serializeinout(k.Key, k.Value)).ToImmutableArray();
            var outputs = TaskInputOutputInfo.Outputs.Select(k => serializeinout(k.Key, k.Value)).ToImmutableArray();

            var watchinginputs = new[]
            {
                serializeval<MPlusWatchingTaskSource>("MPlus"),
                serializeval<LocalWatchingTaskSource>("User"),
                serializeval<OtherUserWatchingTaskSource>("Other Node"),
            }.ToImmutableArray();
            var watchingoutputs = new[]
            {
                serializeval<MPlusWatchingTaskOutputInfo>("MPlus"),
                serializeval<LocalWatchingTaskOutputInfo>("User"),
            }.ToImmutableArray();

            return new TasksFullDescriber(actions, inputs, outputs, watchinginputs, watchingoutputs);


            static TaskActionDescriber serializeaction(IPluginAction action) => new TaskActionDescriber(action.Type, action.Name, (ObjectDescriber) FieldDescriber.Create(action.DataType));
            static TaskInputOutputDescriber serializeinout(TaskInputOutputType tasktype, Type type) => new TaskInputOutputDescriber(tasktype.ToString(), (ObjectDescriber) FieldDescriber.Create(type));
            static TaskInputOutputDescriber serializeval<T>(string name) => new TaskInputOutputDescriber(name, (ObjectDescriber) FieldDescriber.Create(typeof(T)));
        }
    }


    public static IEnumerable<IPluginAction> Get(PluginType type) => Actions.Where(x => x.Type == type);
    public static IPluginAction GetAction(this ReceivedTask task) => task.Info.GetAction();
    public static IPluginAction GetAction(this TaskInfo task) => TryGet(task.TaskType) ?? throw new Exception($"Got an unknown task type: {task.TaskType}");
    public static IPluginAction Get(string name) => Actions.First(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    public static IPluginAction? TryGet(string name) => Actions.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    public static IPluginAction? TryGet(PluginType type, string name) => Actions.FirstOrDefault(x => x.Type == type && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
