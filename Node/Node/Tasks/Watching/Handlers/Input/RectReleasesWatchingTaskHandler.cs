namespace Node.Tasks.Watching.Handlers.Input;

public class RectReleasesWatchingTaskHandler : WatchingTaskInputHandler<RectReleasesWatchingTaskInputInfo>, ITypedTaskWatchingInput
{
    public static WatchingTaskInputType Type => WatchingTaskInputType.RectReleases;
    public required ReleaseRector Rector { get; init; }
    public required PluginChecker PluginChecker { get; init; }
    public required PluginDeployer PluginDeployer { get; init; }
    public required PluginManager PluginManager { get; init; }
    public required PluginList PluginList { get; init; }

    public override void StartListening()
    {
        StartThreadRepeated(60_000, run);


        async Task run()
        {
            if (PluginList.TryGetPlugin(PluginType.ImageDetector) is null)
            {
                var tree = PluginChecker.GetInstallationTree(PluginType.ImageDetector, PluginVersion.Empty);
                PluginDeployer.DeployUninstalled(tree);
                await PluginManager.RediscoverPluginsAsync();
            }

            if (PluginList.TryGetPlugin(PluginType.ImageDetector) is null)
            {
                Logger.LogInformation("Could not find ImageDetector plugin, skipping execution");
                return;
            }

            var exec = await
                from releases in Rector.GetReleasesWithoutRect(Token.Token)
                from downloaded in Rector.DownloadReleases(releases, Token.Token)
                from executed in Rector.ProcessReleases(downloaded, Token.Token)
                select OperationResult.Succ();

            exec.ThrowIfError();
        }
    }
}
