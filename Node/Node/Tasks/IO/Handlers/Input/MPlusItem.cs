namespace Node.Tasks.IO.Handlers.Input;

public static class MPlusItem
{
    public class InputDownloader : TaskInputDownloader<MPlusItemTaskInputInfo, MPlusItemInfo>, ITypedTaskInput
    {
        public static TaskInputType Type => TaskInputType.MPlusItem;

        public required IRegisteredTaskApi ApiTask { get; init; }
        public required Apis Api { get; init; }

        protected override async Task<MPlusItemInfo> DownloadImpl(MPlusItemTaskInputInfo input, TaskObject obj, CancellationToken token) =>
            await Api.ShardGet<MPlusItemInfo>(ApiTask, "gettaskmpitem", "item", "Getting m+ item info", Api.AddSessionId(("taskid", ApiTask.Id), ("iid", input.Iid)))
                .ThrowIfError();
    }
    public class TaskObjectProvider : TaskObjectProvider<MPlusItemTaskInputInfo>, ITypedTaskInput
    {
        public static TaskInputType Type => TaskInputType.MPlusItem;

        public override async Task<OperationResult<TaskObject>> GetTaskObject(MPlusItemTaskInputInfo input, CancellationToken token) =>
            new TaskObject("stub.mp", 1).AsOpResult();
    }
}
