namespace Node.Tasks.Exec;

public class TaskActionList
{
    public IReadOnlyCollection<IPluginActionInfo> AllActions => Actions;
    readonly List<IPluginActionInfo> Actions = new();


    public void Add(params IPluginActionInfo[] actions) => Add(actions.AsEnumerable());
    public void Add(IEnumerable<IPluginActionInfo> actions)
    {
        Actions.AddRange(actions);
        NodeGlobalState.Instance.TaskDefinitions.Value = serializeActions();


        TasksFullDescriber serializeActions()
        {
            return new TasksFullDescriber(
                Actions.Select(serializeaction).ToImmutableArray(),
                serialize(TaskModels.Inputs),
                serialize(TaskModels.Outputs),
                serialize(TaskModels.WatchingInputs),
                serialize(TaskModels.WatchingOutputs)
            );


            static TaskActionDescriber serializeaction(IPluginActionInfo action) => new TaskActionDescriber(action.RequiredPlugins, action.Name.ToString(), new ObjectDescriber(action.DataType));
            static ImmutableArray<TaskInputOutputDescriber> serialize<T>(ImmutableDictionary<T, Type> dict) where T : struct, Enum =>
                dict.Select(x => new TaskInputOutputDescriber(x.Key.ToString(), new ObjectDescriber(x.Value))).ToImmutableArray();
        }
    }
}
