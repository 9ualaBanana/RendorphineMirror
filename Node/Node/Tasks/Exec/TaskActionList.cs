namespace Node.Tasks.Exec;

public class TaskActionList
{
    public IReadOnlyCollection<Type> AllActions => Actions;
    readonly List<Type> Actions = new();


    public void Add(params Type[] actions) => Add(actions.AsEnumerable());
    public void Add(IEnumerable<Type> actions)
    {
        Actions.AddRange(actions.Select(action =>
        {
            if (!action.IsAssignableTo(typeof(IGPluginAction)))
                throw new InvalidOperationException("Invalid type");

            return action;
        }));
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


            static TaskActionDescriber serializeaction(IGPluginAction action) => new TaskActionDescriber(action.RequiredPlugins, action.Name.ToString(), new ObjectDescriber(action.DataType));
            static ImmutableArray<TaskInputOutputDescriber> serialize<T>(ImmutableDictionary<T, Type> dict) where T : struct, Enum =>
                dict.Select(x => new TaskInputOutputDescriber(x.Key.ToString(), new ObjectDescriber(x.Value))).ToImmutableArray();
        }
    }
}
