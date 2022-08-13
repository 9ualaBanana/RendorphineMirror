namespace Common.Tasks;

public record ReceivedTask(string Id, TaskInfo Info, bool ExecuteLocally) : ILoggable
{
    string ILoggable.LogName => $"Task {Id}";

    public static string GenerateLocal() => "local_" + Guid.NewGuid();
}