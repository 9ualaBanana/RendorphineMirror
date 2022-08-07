namespace Common.NodeToUI;

public record ReceivedTask(string Id, TaskInfo Info, bool ExecuteLocally)
{
    public static string GenerateLocal() => "local_" + Guid.NewGuid();
}