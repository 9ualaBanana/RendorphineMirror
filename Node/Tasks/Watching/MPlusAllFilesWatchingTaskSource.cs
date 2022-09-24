namespace Node.Tasks.Watching;

public class MPlusAllFilesWatchingTaskSource : MPlusWatchingTaskSourceBase
{
    public override WatchingTaskInputOutputType Type => WatchingTaskInputOutputType.MPlusAllFiles;

    public MPlusAllFilesWatchingTaskSource(string? sinceiid) : base(sinceiid) { }

    protected override ValueTask<OperationResult<ImmutableArray<MPlusNewItem>>> FetchItemsAsync() =>
        Api.ApiGet<ImmutableArray<MPlusNewItem>>($"{Api.TaskManagerEndpoint}/getnewitems", "items", "Getting new items", ("sessionid", Settings.SessionId!), ("sinceiid", SinceIid ?? string.Empty));
}
