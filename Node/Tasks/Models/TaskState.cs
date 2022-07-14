namespace Node.Tasks.Models;

public enum TaskState
{
    Queued,
    Input,
    Active,
    Output,
    Finished,
    Canceled,
    Failed,
}
