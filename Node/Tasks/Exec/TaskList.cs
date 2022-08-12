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

        NodeGlobalState.Instance.TaskDefinitions.Value = serializeActions();


        TasksFullDescriber serializeActions()
        {
            var actions = Actions.Select(serialize).ToImmutableArray();
            var inputs = new[]
            {
                serializeinout<MPlusTaskInputInfo>(TaskInputOutputType.MPlus),
                serializeinout<UserTaskInputInfo>(TaskInputOutputType.User),
            }.ToImmutableArray();
            var outputs = new[]
            {
                serializeinout<MPlusTaskOutputInfo>(TaskInputOutputType.MPlus),
                serializeinout<UserTaskOutputInfo>(TaskInputOutputType.User),
            }.ToImmutableArray();
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


            static TaskActionDescriber serialize(IPluginAction action) => new TaskActionDescriber(action.Type, action.Name, (ObjectDescriber) FieldDescriber.Create(action.DataType));
            static TaskInputOutputDescriber serializeinout<T>(TaskInputOutputType type) => serializeval<T>(type.ToString());
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
