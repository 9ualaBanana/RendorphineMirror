namespace Common.NodeToUI;

public class TasksFullDescriber
{
    public readonly ImmutableArray<TaskActionDescriber> Actions;
    public readonly ImmutableArray<TaskInputOutputDescriber> Inputs, Outputs;
    public readonly ImmutableArray<TaskInputOutputDescriber> WatchingInputs;

    public TasksFullDescriber(ImmutableArray<TaskActionDescriber> actions, ImmutableArray<TaskInputOutputDescriber> inputs,
        ImmutableArray<TaskInputOutputDescriber> outputs, ImmutableArray<TaskInputOutputDescriber> watchingInputs)
    {
        Actions = actions;
        Inputs = inputs;
        Outputs = outputs;
        WatchingInputs = watchingInputs;
    }
}
