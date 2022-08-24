namespace NodeToUI;

public class TasksFullDescriber
{
    public readonly ImmutableArray<TaskActionDescriber> Actions;
    public readonly ImmutableArray<TaskInputOutputDescriber> Inputs, Outputs;
    public readonly ImmutableArray<TaskInputOutputDescriber> WatchingInputs, WatchingOutputs;

    public TasksFullDescriber(ImmutableArray<TaskActionDescriber> actions, ImmutableArray<TaskInputOutputDescriber> inputs,
        ImmutableArray<TaskInputOutputDescriber> outputs, ImmutableArray<TaskInputOutputDescriber> watchingInputs, ImmutableArray<TaskInputOutputDescriber> watchingOutputs)
    {
        Actions = actions;
        Inputs = inputs;
        Outputs = outputs;
        WatchingInputs = watchingInputs;
        WatchingOutputs = watchingOutputs;
    }
}
