namespace Node.Tasks.Watching;

public class MPlusWatchingTaskSource : MPlusWatchingTaskSourceBase
{
    public override WatchingTaskInputOutputType Type => WatchingTaskInputOutputType.MPlus;

    [MPlusDirectory] public readonly string Directory;

    public MPlusWatchingTaskSource(string directory, string? sinceIid) : base(sinceIid) => Directory = directory;

    protected override ValueTask<OperationResult<ImmutableArray<MPlusNewItem>>> FetchItemsAsync() =>
        Api.ApiGet<ImmutableArray<MPlusNewItem>>($"{Api.TaskManagerEndpoint}/getmynewitems", "items", "Getting new items", ("sessionid", Settings.SessionId!), ("sinceiid", SinceIid ?? string.Empty), ("directory", Directory));
}
