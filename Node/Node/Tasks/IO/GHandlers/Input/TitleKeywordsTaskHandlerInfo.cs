using Node.Tasks.Exec.Actions;

namespace Node.Tasks.IO.GHandlers.Input;

public class TitleKeywordsTaskHandlerInfo : TaskInputHandlerInfo<TitleKeywordsInputInfo, TitleKeywords>
{
    public override TaskInputType Type => TaskInputType.TitleKeywords;
    protected override Type HandlerType => typeof(Handler);
    protected override Type TaskObjectProviderType => typeof(TaskObjectProvider);


    class Handler : HandlerBase
    {
        public override Task<TitleKeywords> Download(TitleKeywordsInputInfo input, TaskObject obj, CancellationToken token) =>
            new TitleKeywords(input.Title, input.Keywords.ToImmutableArray()).AsTask();
    }
    class TaskObjectProvider : TaskObjectProviderBase
    {
        public override Task<OperationResult<TaskObject>> GetTaskObject(TitleKeywordsInputInfo input, CancellationToken token) =>
            new TaskObject("tk.tk", 0).AsOpResult().AsTask();
    }
}
