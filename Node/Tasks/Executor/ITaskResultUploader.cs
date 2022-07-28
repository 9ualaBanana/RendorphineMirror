namespace Node.Tasks.Executor;

public interface ITaskResultUploader
{
    Task Upload(ReceivedTask task, string file);
}
