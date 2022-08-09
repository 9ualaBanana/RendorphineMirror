namespace Machine.Plugins.Deployment;

public class PluginsDeployer
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    readonly HttpClient _httpClient;
    readonly CancellationToken _cancellationToken;

    public PluginsDeployer(HttpClient httpClient, CancellationToken cancellationToken = default)
    {
        _httpClient = httpClient;
        _cancellationToken = cancellationToken;
    }

    public async Task DeployAsync(PluginDeploymentInfo deploymentInfo, bool deleteInstaller = true)
    {
        if (!File.Exists(deploymentInfo.InstallerPath))
            await DownloadAsync(deploymentInfo);
        if (deploymentInfo.RequiresInstallation)
            await InstallAsync(deploymentInfo);

        if (deploymentInfo.RequiresInstallation && deleteInstaller) File.Delete(deploymentInfo.InstallerPath);
    }

    async Task DownloadAsync(PluginDeploymentInfo deploymentInfo)
    {
        try
        {
            _logger.Debug("Downloading plugin from {Url}", deploymentInfo.DownloadUrl);
            await DownloadAsyncCore(deploymentInfo);
            _logger.Debug("Plugin is downloaded to {Path}", deploymentInfo.InstallerPath);
        }
        catch (Exception ex) { _logger.Error(ex, "Plugin couldn't be downloaded"); throw; }
    }

    async Task DownloadAsyncCore(PluginDeploymentInfo deploymentInfo)
    {
        var pluginInstaller = await _httpClient.GetStreamAsync(deploymentInfo.DownloadUrl, _cancellationToken);
        using var fs = new FileStream(deploymentInfo.InstallerPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true);
        await pluginInstaller.CopyToAsync(fs, _cancellationToken);
    }

    async Task InstallAsync(PluginDeploymentInfo deploymentInfo)
    {
        try
        {
            _logger.Debug("Installing the plugin from {InstallerPath}", deploymentInfo.InstallerPath);
            await deploymentInfo.Installation!(_cancellationToken);
            _logger.Debug("Plugin is installed");
        }
        catch (Exception ex) { _logger.Error(ex, "Plugin couldn't be installed"); throw; }
    }
}
