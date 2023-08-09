namespace Node.Tasks.IO.GHandlers.Input;

public static class TitleKeywords
{
    public class InputDownloader : TaskInputDownloader<TitleKeywordsInputInfo, Exec.Actions.TitleKeywords>, ITypedTaskInput
    {
        public static TaskInputType Type => TaskInputType.TitleKeywords;

        protected override Task<Exec.Actions.TitleKeywords> DownloadImpl(TitleKeywordsInputInfo input, TaskObject obj, CancellationToken token) =>
            new Exec.Actions.TitleKeywords(input.Title, input.Keywords.ToImmutableArray()).AsTask();
    }
    public class TaskObjectProvider : TaskObjectProvider<TitleKeywordsInputInfo>, ITypedTaskInput
    {
        public static TaskInputType Type => TaskInputType.TitleKeywords;

        public override Task<OperationResult<TaskObject>> GetTaskObject(TitleKeywordsInputInfo input, CancellationToken token) =>
            new TaskObject("tk.tk", 0).AsOpResult().AsTask();
    }
}
