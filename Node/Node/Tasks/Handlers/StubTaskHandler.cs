namespace Node.Tasks.Handlers;

public class StubTaskHandler : ITaskInputHandler
{
    TaskInputType ITaskInputHandler.Type => TaskInputType.Stub;

    public ValueTask<ReadOnlyTaskFileList> Download(ReceivedTask task, CancellationToken cancellationToken = default) =>
        new ReadOnlyTaskFileList(Enumerable.Empty<FileWithFormat>()).AsVTask();
    public ValueTask<OperationResult<TaskObject>> GetTaskObject(ITaskInputInfo input) =>
        new TaskObject("stub.stub", 123).AsTaskResult();
}
