namespace Node.Tasks.IO.GHandlers.Input;

public class StubTaskHandlerInfo : FileTaskInputHandlerInfo<StubTaskInfo>
{
    public override TaskInputType Type => TaskInputType.Stub;
    protected override Type HandlerType => typeof(Handler);
    protected override Type TaskObjectProviderType => typeof(TaskObjectProvider);


    class Handler : HandlerBase
    {
        public override Task<ReadOnlyTaskFileList> Download(StubTaskInfo input, TaskObject obj, CancellationToken token) =>
            new ReadOnlyTaskFileList(Enumerable.Empty<FileWithFormat>()).AsTask();
    }
    class TaskObjectProvider : TaskObjectProviderBase
    {
        public override Task<OperationResult<TaskObject>> GetTaskObject(StubTaskInfo input, CancellationToken token) =>
            new TaskObject("stub.stub", 0).AsOpResult().AsTask();
    }
}
