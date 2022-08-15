namespace Common.Tasks;

public record ReceivedTask(string Id, TaskInfo Info, bool ExecuteLocally) : IHasTaskId
{
    public static string GenerateLocal() => "local_" + Guid.NewGuid();
}