namespace Node.Plugins.Deployment;

public abstract record DownloadablePluginDeploymentInfo(string? InstallationPath = null) : PluginDeploymentInfo(InstallationPath)
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    public string InstallerPath => Path.Combine(DownloadsDirectoryPath, Path.GetFileName(DownloadUrl));
    public abstract string DownloadUrl { get; }
    public bool RequiresInstallation => Installation is not null;
    public virtual Func<CancellationToken, Task>? Installation { get; }

    public override async Task DeployAsync(bool deleteInstaller = true)
    {
        if (!File.Exists(InstallerPath))
            await DownloadAsync();
        if (RequiresInstallation)
            await InstallAsync();

        if (RequiresInstallation && deleteInstaller) File.Delete(InstallerPath);
    }

    async Task DownloadAsync()
    {
        try
        {
            _logger.Debug("Downloading plugin from {Url}", DownloadUrl);
            await DownloadAsyncCore();
            _logger.Debug("Plugin is downloaded to {Path}", InstallerPath);
        }
        catch (Exception ex) { _logger.Error(ex, "Plugin couldn't be downloaded"); throw; }
    }

    async Task DownloadAsyncCore()
    {
        var pluginInstaller = await new HttpClient().GetStreamAsync(DownloadUrl, CancellationToken.None);
        using var fs = new FileStream(InstallerPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true);
        await pluginInstaller.CopyToAsync(fs, CancellationToken.None);
    }

    async Task InstallAsync()
    {
        try
        {
            _logger.Debug("Installing the plugin from {InstallerPath}", InstallerPath);
            await Installation!(default);
            _logger.Debug("Plugin is installed");
        }
        catch (Exception ex) { _logger.Error(ex, "Plugin couldn't be installed"); throw; }
    }
}
