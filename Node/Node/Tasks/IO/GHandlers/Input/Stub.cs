namespace Node.Tasks.IO.GHandlers.Input;

public static class Stub
{
    public class InputDownloader : FileTaskInputDownloader<StubTaskInfo>, ITypedTaskInput
    {
        public static TaskInputType Type => TaskInputType.Stub;

        protected override Task<ReadOnlyTaskFileList> DownloadImpl(StubTaskInfo input, TaskObject obj, CancellationToken token) =>
            new ReadOnlyTaskFileList(Enumerable.Empty<FileWithFormat>()).AsTask();
    }
    public class TaskObjectProvider : TaskObjectProvider<StubTaskInfo>, ITypedTaskInput
    {
        public static TaskInputType Type => TaskInputType.Stub;

        public override Task<OperationResult<TaskObject>> GetTaskObject(StubTaskInfo input, CancellationToken token) =>
            new TaskObject("stub.stub", 0).AsOpResult().AsTask();
    }
}
