namespace Machine.Plugins.Deployment;

public class PluginsDeployer
{
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
            await deploymentInfo.Installation!(_cancellationToken);

        if (deploymentInfo.RequiresInstallation && deleteInstaller) File.Delete(deploymentInfo.InstallerPath);
    }

    async Task DownloadAsync(PluginDeploymentInfo deploymentInfo)
    {
        var pluginInstaller = await _httpClient.GetStreamAsync(deploymentInfo.DownloadUrl, _cancellationToken);
        using var fs = new FileStream(deploymentInfo.InstallerPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true);
        await pluginInstaller.CopyToAsync(fs, _cancellationToken);
    }
}
