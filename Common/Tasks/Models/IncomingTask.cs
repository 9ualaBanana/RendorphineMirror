namespace Common.Tasks.Models;

public record IncomingTask(
    string TaskId,
    TaskInfo Task)
{
    public bool HasMPlusInput => Task.Input.GetProperty("type").GetString() == "MPlus";
}
