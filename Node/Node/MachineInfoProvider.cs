namespace Node;

[AutoRegisteredService(true)]
public class MachineInfoProvider
{
    public required SettingsInstance Settings { get; init; }
    public required PluginManager PluginManager { get; init; }
    public required Init Init { get; init; }

    public async Task<MachineInfo> GetAsync()
    {
        return new MachineInfo(
            Settings.UserId,
            Settings.NodeName,
            Environment.UserName,
            Environment.MachineName,
            Settings.Guid,
            Init.Version,
            Settings.UPnpPort.ToStringInvariant(),
            Settings.UPnpServerPort.ToStringInvariant(),
            (await PortForwarding.GetPublicIPAsync()).ToString(),
            (await PluginManager.GetInstalledPluginsAsync()).ToImmutableArray()
        );
    }
}
