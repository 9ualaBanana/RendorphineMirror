using System.Diagnostics;

namespace Machine.Plugins.Installers;

internal class PluginsDeployer
{
    readonly HttpClient _httpClient;
    readonly CancellationToken _cancellationToken;

    internal PluginsDeployer(HttpClient httpClient, CancellationToken cancellationToken = default)
    {
        _httpClient = httpClient;
        _cancellationToken = cancellationToken;
    }

    internal async Task DeployAsync(PluginDeploymentInfo deploymentInfo, bool deleteInstaller = true)
    {
        if (!File.Exists(deploymentInfo.InstallerPath))
            await DownloadAsync(deploymentInfo);
        await InstallAsync(deploymentInfo);

        if (deleteInstaller) File.Delete(deploymentInfo.InstallerPath);
    }

    async Task DownloadAsync(PluginDeploymentInfo deploymentInfo)
    {
        var pluginInstaller = await _httpClient.GetStreamAsync(deploymentInfo.DownloadUrl, _cancellationToken);
        using var fs = new FileStream(deploymentInfo.InstallerPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true);
        await pluginInstaller.CopyToAsync(fs, _cancellationToken);
    }

    async Task InstallAsync(PluginDeploymentInfo deploymentInfo)
    {
        using var installation = Process.Start(deploymentInfo.InstallationStartInfo)!;
        await installation.WaitForExitAsync(_cancellationToken);
    }
}
