namespace Common.Tasks.Tasks.DTO;

public record TaskInfo
{
    public string Type { get; }

    public TaskInfo(TaskType type = TaskType.User)
    {
        Type = Enum.GetName(type)!;
    }
}
