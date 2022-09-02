namespace Common.Tasks;

public record PlacedTask(string Id, TaskCreationInfo Info) : ITask
{
    string ILoggable.LogName => $"Task {Id}";
    public TaskState State = TaskState.Input;

    public bool ExecuteLocally => Info.ExecuteLocally;
}