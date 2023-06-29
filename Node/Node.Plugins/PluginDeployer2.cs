using System.Management.Automation;

namespace Node.Plugins;

// TODO: non static
public class PluginDeployer2
{
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();


    /// <param name="version"> Plugin version or null if any </param>
    public static bool IsInstalled(PluginType type, PluginVersion version, IReadOnlyCollection<Plugin> installedPlugins) =>
        installedPlugins.Any(i => i.Type == type && (version.IsEmpty || i.Version == version));


    /// <remarks>
    /// Deploys only specified plugins;
    /// To get plugin list with parents, use <see cref="PluginChecker.GetInstallationTree(IEnumerable{PluginToDeploy}, IReadOnlyDictionary{string, SoftwareDefinition})"/>.
    /// Deploys only non-installed plugins.
    /// </remarks>
    /// <returns> Amount of installed plugins </returns>
    public static async Task<int> DeployUninstalledAsync(IEnumerable<PluginToInstall> plugins, IReadOnlyCollection<Plugin> installedPlugins)
    {
        var uninstalled = plugins.Where(plugin => !IsInstalled(plugin.Type, plugin.Version, installedPlugins)).ToArray();

        if (uninstalled.Any(p => p.InstallScript is null))
            throw new Exception($"Plugins {string.Join(", ", uninstalled.Where(p => p.InstallScript is null))} can't be and aren't installed");

        var installed = 0;
        foreach (var plugin in plugins)
        {
            await DeployAsync(plugin.Type, plugin.Version, plugin.InstallScript.ThrowIfNull());
            installed++;
        }

        return installed;
    }

    /// <remarks>
    /// Deploys only specified plugins;
    /// To get plugin list with parents, use <see cref="PluginChecker.GetInstallationTree(IEnumerable{PluginToDeploy}, IReadOnlyDictionary{string, SoftwareDefinition})"/>.
    /// Deploys even if already installed.
    /// </remarks>
    public static async Task DeployAsync(PluginType type, PluginVersion version, string script)
    {
        Logger.Info($"Installing {type} {version}");

        await Task.Run(() =>
        {
            var pwsh = PowerShellInvoker.Initialize(script);
            var pldownload = setVars(pwsh);
            var result = PowerShellInvoker.Invoke(pwsh);

            if (Directory.Exists(pldownload))
                Directory.Delete(pldownload, true);
        });


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

            var pldownload = Directories.Created(Path.Combine(prox.GetVariable("DOWNLOADS").ToString()!, plugintype, pluginverpath));
            prox.SetVariable("PLDOWNLOAD", pldownload);
            prox.SetVariable("PLINSTALL", Directories.Created(Path.Combine(prox.GetVariable("PLUGINS").ToString()!, plugintype, pluginverpath)));

            return pldownload;
        }
    }
}
