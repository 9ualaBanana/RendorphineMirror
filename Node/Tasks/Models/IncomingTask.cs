namespace Node.Tasks.Models;

public record IncomingTask(
    string TaskId,
    TaskInfo Task)
{
    internal bool HasMPlusInput => Task.Input.GetProperty("type").GetString() == "MPlus";
}
