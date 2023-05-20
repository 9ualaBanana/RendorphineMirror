namespace Node.Tasks;

public record CompletedTask(DateTimeOffset StartTime, DateTimeOffset FinishTime, ReceivedTask TaskInfo)
{
    public int Attempt = 0;
}