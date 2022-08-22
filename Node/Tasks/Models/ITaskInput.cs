namespace Node.Tasks.Models;

public interface ITaskInput
{
    ValueTask Upload();
    ValueTask<string> Download(ReceivedTask task, CancellationToken cancellationToken);
}
