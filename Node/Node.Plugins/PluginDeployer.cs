using System.Management.Automation;

namespace Node.Plugins;

public class PluginDeployer
{
    public required PowerShellInvoker PowerShellInvoker { get; init; }
    public required IInstalledPluginsProvider InstalledPlugins { get; init; }
    public required CondaManager CondaManager { get; init; }
    public required ILogger<PluginDeployer> Logger { get; init; }


    /// <param name="version"> Plugin version or null if any </param>
    public bool IsInstalled(PluginType type, PluginVersion version) => IsInstalled(InstalledPlugins.Plugins, type, version);

    /// <inheritdoc cref="IsInstalled(PluginType, PluginVersion)"/>
    public static bool IsInstalled(IReadOnlyCollection<Plugin> installed, PluginType type, PluginVersion version) =>
        installed.Any(i => i.Type == type && (version.IsEmpty || i.Version == version));


    /// <remarks>
    /// Deploys only specified plugins;
    /// To get plugin list with parents, use <see cref="PluginChecker.GetInstallationTree(ImmutableDictionary{string, ImmutableDictionary{PluginVersion, SoftwareVersionInfo}}, IEnumerable{PluginToDeploy})"/>.
    /// Deploys only non-installed plugins.
    /// </remarks>
    /// <returns> Amount of installed plugins </returns>
    public int DeployUninstalled(IEnumerable<PluginToInstall> plugins)
    {
        var uninstalled = plugins.Where(plugin => !IsInstalled(plugin.Type, plugin.Version)).ToArray();

        if (uninstalled.Any(p => p.Installation is null))
            throw new Exception($"Plugins {string.Join(", ", uninstalled.Where(p => p.Installation is null))} are not installed and can't be");

        var installed = 0;
        foreach (var plugin in uninstalled)
        {
            Deploy(plugin.Type, plugin.Version, plugin.Installation.ThrowIfNull($"Plugin {plugin} somehow has null installScript"));
            installed++;
        }

        return installed;
    }

    /// <remarks> Deploys even if installed </remarks>
    public void Deploy(PluginType type, PluginVersion version, SoftwareVersionInfo.InstallationInfo installation)
    {
        Logger.LogInformation($"Installing {type} {version}");

        if (installation.Script is not null)
            InstallWithPowershell(type, version, installation.Script);
        if (installation.Python is not null)
            InstallWithConda(type, version, installation.Python);

        Logger.LogInformation($"Installed {type} {version}");
    }

    void InstallWithPowershell(PluginType type, PluginVersion version, string script)
    {
        var pwsh = PowerShellInvoker.Initialize(script);
        var pldownload = setVars(pwsh);
        var result = PowerShellInvoker.Invoke(pwsh);

        if (Directory.Exists(pldownload))
            Directory.Delete(pldownload, true);


        /// <returns> Plugin download directory </returns>
        string setVars(PowerShell psh)
        {
            var prox = psh.Runspace.SessionStateProxy;
            var plugintype = type.ToString().ToLowerInvariant();
            var pluginver = version.ToString();

            var pluginverpath = pluginver;
            foreach (var invalid in Path.GetInvalidFileNameChars())
                pluginverpath = pluginverpath.Replace(invalid, '_');


            prox.SetVariable("PLUGIN", plugintype);
            prox.SetVariable("PLUGINVER", pluginver);

            prox.SetVariable("PLTORRENT", plugintype + "." + pluginverpath);

            var pldownload = Directories.DirCreated(Path.Combine(prox.GetVariable("DOWNLOADS").ToString()!, plugintype, pluginverpath));
            prox.SetVariable("PLDOWNLOAD", pldownload);
            prox.SetVariable("PLINSTALL", Directories.DirCreated(Path.Combine(prox.GetVariable("PLUGINS").ToString()!, plugintype, pluginverpath)));

            return pldownload;
        }
    }
    void InstallWithConda(PluginType type, PluginVersion version, SoftwareVersionInfo.InstallationInfo.PythonInfo info)
    {
        var name = $"{type.ToString().ToLowerInvariant()}_{version}";
        var condapath = InstalledPlugins.Plugins.First(p => p.Type == PluginType.Conda).Path;

        CondaManager.InitializeEnvironment(condapath, name, info.Version, info.Conda.Requirements, info.Conda.Channels, info.Pip.Requirements, info.Pip.RequirementFiles);
    }
}
