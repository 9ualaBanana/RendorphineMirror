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


            var releases = await Rector.GetReleasesWithoutRect(Token.Token).ThrowIfError();
            foreach (var release in releases)
            {
                var downloaded = await Rector.DownloadRelease(release, Token.Token).ThrowIfError();

                try
                {
                    await Rector.ProcessRelease(release, downloaded, Token.Token).ThrowIfError();
                }
                finally
                {
                    if (downloaded is not null)
                        File.Delete(downloaded);
                }
            }
        }
    }
}
