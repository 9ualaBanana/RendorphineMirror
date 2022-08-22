namespace Node.Tasks.Models;

public interface ITaskOutput
{
    ValueTask Upload(ReceivedTask task, string file);
}
