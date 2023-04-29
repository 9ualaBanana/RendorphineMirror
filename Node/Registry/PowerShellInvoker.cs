using System.Management.Automation;

namespace Node.Registry;

public static class PowerShellPluginInstaller
{
    public static readonly Logger Logger = LogManager.GetLogger("PLUGINS");

    public static IReadOnlyCollection<PSObject> Install(PluginToDeploy plugin, string script, bool deleteInstaller)
    {
        var pwsh = PowerShellInvoker.Initialize(script);
        var pldownload = setVars(pwsh);
        var result = PowerShellInvoker.Invoke(pwsh);

        if (deleteInstaller && Directory.Exists(pldownload))
            Directory.Delete(pldownload, true);

        return result;


        /// <returns> Plugin download directory </returns>
        string setVars(PowerShell psh)
        {
            var prox = psh.Runspace.SessionStateProxy;
            var plugintype = plugin.Type.ToString().ToLowerInvariant();
            var pluginver = plugin.Version;
            if (pluginver.Length == 0 && NodeGlobalState.Instance.Software.Value.TryGetValue(plugintype, out var vers))
                pluginver = vers.Versions.Keys.MaxBy(PluginVersion.Parse) ?? pluginver;

            Path.GetInvalidFileNameChars().ToList().ForEach(x => pluginver = pluginver.Replace(x, '_'));

            prox.SetVariable("PLUGIN", plugintype);
            prox.SetVariable("PLUGINVER", pluginver);

            prox.SetVariable("PLTORRENT", plugintype + "." + pluginver);

            var pldownload = Directories.Created(Path.Combine(prox.GetVariable("DOWNLOADS").ToString()!, plugintype, pluginver));
            prox.SetVariable("PLDOWNLOAD", pldownload);
            prox.SetVariable("PLINSTALL", Directories.Created(Path.Combine(prox.GetVariable("PLUGINS").ToString()!, plugintype, pluginver)));

            return pldownload;
        }
    }

}
