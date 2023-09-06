using Node.Tasks.Exec.Actions;

namespace Node.Tasks.IO.Handlers.Input;

public static class TitleKeywords
{
    public class InputDownloader : TaskInputDownloader<TitleKeywordsInputInfo, TitleKeywordsInput>, ITypedTaskInput
    {
        public static TaskInputType Type => TaskInputType.TitleKeywords;

        protected override Task<TitleKeywordsInput> DownloadImpl(TitleKeywordsInputInfo input, TaskObject obj, CancellationToken token) =>
            new TitleKeywordsInput(input.Title, input.Keywords.ToImmutableArray()).AsTask();
    }
    public class TaskObjectProvider : TaskObjectProvider<TitleKeywordsInputInfo>, ITypedTaskInput
    {
        public static TaskInputType Type => TaskInputType.TitleKeywords;

        public override Task<OperationResult<TaskObject>> GetTaskObject(TitleKeywordsInputInfo input, CancellationToken token) =>
            new TaskObject("tk.tk", 0).AsOpResult().AsTask();
    }
}
