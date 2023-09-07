namespace Node.Tasks.Watching.Handlers.Input;

public class RectReleasesWatchingTaskHandler : WatchingTaskInputHandler<RectReleasesWatchingTaskInputInfo>, ITypedTaskWatchingInput
{
    public static WatchingTaskInputType Type => WatchingTaskInputType.RectReleases;
    public required ReleaseRector Rector { get; init; }

    public override void StartListening()
    {
        StartThreadRepeated(60_000, run);


        async Task run()
        {
            var exec = await
                from releases in Rector.GetReleasesWithoutRect(Token.Token)
                from downloaded in Rector.DownloadReleases(releases, Token.Token)
                from executed in Rector.ProcessReleases(downloaded, Token.Token)
                select OperationResult.Succ();

            exec.ThrowIfError();
        }
    }
}
