namespace Node.Heartbeat;

public sealed class UserSettingsHeartbeat : Heartbeat
{
    protected override TimeSpan Interval { get; } = TimeSpan.FromMinutes(1);

    readonly PluginManager PluginManager;
    readonly PluginChecker PluginChecker;
    readonly PluginDeployer PluginDeployer;
    readonly NodeCommon.Apis Api;

    bool IsDeploying = false;

    public UserSettingsHeartbeat(PluginManager pluginManager, PluginChecker pluginChecker, PluginDeployer pluginDeployer, NodeCommon.Apis api, ILogger<UserSettingsHeartbeat> logger) : base(logger)
    {
        PluginManager = pluginManager;
        PluginChecker = pluginChecker;
        PluginDeployer = pluginDeployer;
        Api = api;
    }

    protected override async Task Execute()
    {
        if (IsDeploying) return;
        using var _ = new FuncDispose(() => IsDeploying = false);
        IsDeploying = true;

        var settings = await Api.GetSettingsAsync()
            .ThrowIfError($"{this} was unable to deploy uninstalled plugins: {{0}}");

        await trydeploy(settings.InstallSoftware);
        await trydeploy(settings.GetNodeInstallSoftware(Settings.Guid));


        async Task trydeploy(UUserSettings.TMServerSoftware? software)
        {
            if (software is null) return;

            var newcount = PluginDeployer.DeployUninstalled(PluginChecker.GetInstallationTree(UUserSettings.ToDeploy(software)));
            if (newcount != 0)
                await PluginManager.RediscoverPluginsAsync();
        }
    }
}
