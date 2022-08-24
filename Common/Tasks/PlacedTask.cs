namespace Common.Tasks;

public record PlacedTask(string Id, TaskCreationInfo Info) : ITask
{
    string ILoggable.LogName => $"Task {Id}";
    public bool ExecuteLocally => Info.ExecuteLocally;


    public TaskState State = TaskState.Input;
}