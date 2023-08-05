namespace Node.Tasks.Watching;

public class MPlusWatchingTaskHandler : MPlusWatchingTaskHandler<MPlusWatchingTaskInputInfo>
{
    public override WatchingTaskInputType Type => WatchingTaskInputType.MPlus;

    public MPlusWatchingTaskHandler(WatchingTask task) : base(task) { }

    protected override ValueTask<OperationResult<ImmutableArray<MPlusNewItem>>> FetchItemsAsync() =>
        Api.Default.ApiGet<ImmutableArray<MPlusNewItem>>($"{Api.TaskManagerEndpoint}/getmynewitems", "items", "Getting new items",
            ("sessionid", Settings.SessionId!), ("sinceiid", Input.SinceIid ?? string.Empty), ("directory", Input.Directory));
}