namespace Node.Tasks.Watching.Handlers.Input;

public class MPlusWatchingTaskHandler : MPlusWatchingTaskHandlerBase<MPlusWatchingTaskInputInfo>, ITypedTaskWatchingInput
{
    public static WatchingTaskInputType Type => WatchingTaskInputType.MPlus;

    protected override async Task<OperationResult<ImmutableArray<MPlusNewItem>>> FetchItemsAsync() =>
        await Api.ApiGet<ImmutableArray<MPlusNewItem>>($"{Api.TaskManagerEndpoint}/getmynewitems", "items", "Getting new items",
            ("sessionid", Settings.SessionId!), ("sinceiid", Input.SinceIid ?? string.Empty), ("directory", Input.Directory));
}