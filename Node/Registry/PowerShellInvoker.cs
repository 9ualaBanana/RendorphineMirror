using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace Node.Registry;

public static class PowerShellInvoker
{
    public static readonly Logger Logger = LogManager.GetLogger("PLUGINS");

    public static IReadOnlyCollection<PSObject> InstallPlugin(PluginToDeploy plugin, string script, bool deleteInstaller)
    {
        var pwsh = Initialize(script);
        var pldownload = setVars(pwsh);
        var result = Invoke(pwsh);

        if (deleteInstaller && Directory.Exists(pldownload))
            Directory.Delete(pldownload, true);

        return result;


        /// <returns> Plugin download directory </returns>
        string setVars(PowerShell psh)
        {
            var prox = psh.Runspace.SessionStateProxy;
            var plugintype = plugin.Type.ToString().ToLowerInvariant();
            var pluginver = plugin.Version;
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


    public static PowerShell Initialize(string script)
    {
        var runspace = RunspaceFactory.CreateRunspace();
        // TODO: restrictions on commands?


        runspace.Open();
        AddVariables(runspace);

        var psh = PowerShell.Create(runspace);

        psh.AddStatement()
            .AddCommand("Import-Module")
            .AddParameter("Name", typeof(PowerShellInvoker).Assembly.Location);

        psh.AddStatement()
            .AddScript(script);

        return psh;
    }
    public static Collection<PSObject> Invoke(PowerShell psh)
    {
        var result = psh.Invoke(Enumerable.Empty<object>(), new PSInvocationSettings() { ErrorActionPreference = ActionPreference.Stop });
        if (psh.InvocationStateInfo.Reason is not null)
            throw psh.InvocationStateInfo.Reason;

        foreach (var err in psh.Streams.Error)
            throw err.Exception;

        return result;
    }
    public static Collection<PSObject> Invoke(string script) => Invoke(Initialize(script));

    public static Collection<PSObject> JustInvoke(string script) => PowerShell.Create().AddScript(script).Invoke();
    public static Collection<T> JustInvoke<T>(string script) => PowerShell.Create().AddScript(script).Invoke<T>();


    static void AddVariables(Runspace runspace)
    {
        var prox = runspace.SessionStateProxy;

        prox.SetVariable("PLUGINS", Directories.Created(Path.GetFullPath("plugins")));

        prox.SetVariable("DOWNLOADS", Directories.Created(Path.Combine(Init.ConfigDirectory, "downloads")));
        prox.SetVariable("LOCALAPPDATA", Directories.Created(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)));
        // TODO: other
    }
}
