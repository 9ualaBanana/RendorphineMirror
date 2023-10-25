namespace Node.Heartbeat;

public sealed class UserSettingsHeartbeat : Heartbeat
{
    protected override TimeSpan Interval { get; } = TimeSpan.FromMinutes(1);

    public required PluginManager PluginManager { get; init; }
    public required PluginDeployer PluginDeployer { get; init; }
    public required NodeGlobalState NodeGlobalState { get; init; }
    public required Apis Api { get; init; }

    bool IsDeploying = false;

    public UserSettingsHeartbeat(ILogger<UserSettingsHeartbeat> logger) : base(logger) { }

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

            var newcount = await PluginDeployer.DeployUninstalled(PluginChecker.GetInstallationTree(NodeGlobalState.Software.Value, UUserSettings.ToDeploy(software)), default);
            if (newcount != 0)
                await PluginManager.RediscoverPluginsAsync();
        }
    }
}
