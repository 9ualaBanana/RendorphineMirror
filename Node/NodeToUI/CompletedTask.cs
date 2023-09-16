namespace NodeToUI;

public record CompletedTask(DateTimeOffset StartTime, DateTimeOffset FinishTime, ReceivedTask TaskInfo)
{
    public int Attempt { get; init; } = 0;
}