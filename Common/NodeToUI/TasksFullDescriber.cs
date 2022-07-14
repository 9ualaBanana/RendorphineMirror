namespace Common.NodeToUI;

public class TasksFullDescriber
{
    public readonly ImmutableArray<TaskActionDescriber> Actions;
    public readonly ImmutableArray<TaskInputOutputDescriber> Inputs, Outputs;

    public TasksFullDescriber(ImmutableArray<TaskActionDescriber> actions, ImmutableArray<TaskInputOutputDescriber> inputs, ImmutableArray<TaskInputOutputDescriber> outputs)
    {
        Actions = actions;
        Inputs = inputs;
        Outputs = outputs;
    }
}
