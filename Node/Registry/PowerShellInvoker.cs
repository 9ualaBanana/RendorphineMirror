using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;
using Node.Plugins.Deployment;

namespace Node.Registry;

public static class PowerShellInvoker
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    public static IReadOnlyCollection<PSObject> InstallPlugin(PluginToDeploy plugin, string script, bool deleteInstaller)
    {
        string pldownload = null!;
        var invok = Invoke(script, setVars);

        if (deleteInstaller && Directory.Exists(pldownload))
            Directory.Delete(pldownload, true);

        return invok;


        void setVars(PowerShell psh)
        {
            var prox = psh.Runspace.SessionStateProxy;
            var plugintype = plugin.Type.ToString().ToLowerInvariant();
            var pluginver = plugin.Version;
            Path.GetInvalidFileNameChars().ToList().ForEach(x => pluginver = pluginver.Replace(x, '_'));

            prox.SetVariable("PLTORRENT", plugintype + "." + pluginver);

            prox.SetVariable("PLDOWNLOAD", pldownload = Path.Combine(prox.GetVariable("DOWNLOADS").ToString()!, plugintype, pluginver));
            prox.SetVariable("PLINSTALL", Path.Combine(prox.GetVariable("PLUGINS").ToString()!, plugintype, pluginver));
        }
    }

    public static IReadOnlyCollection<PSObject> Invoke(string script, Action<PowerShell>? runspaceModifFunc = null)
    {
        var runspace = RunspaceFactory.CreateRunspace();
        // TODO: restrictions on commands?


        runspace.Open();
        AddVariables(runspace);

        var psh = PowerShell.Create(runspace);
        runspaceModifFunc?.Invoke(psh);

        psh.AddStatement()
            .AddCommand("Import-Module")
            .AddParameter("Name", typeof(PowerShellInvoker).Assembly.Location);

        psh.AddStatement()
            .AddScript(script);

        var result = psh.Invoke(Array.Empty<object>(), new PSInvocationSettings() { ErrorActionPreference = ActionPreference.Stop });
        if (psh.InvocationStateInfo.Reason is not null)
            throw psh.InvocationStateInfo.Reason;

        foreach (var err in psh.Streams.Error)
            throw err.Exception;

        return result;
    }


    static void AddVariables(Runspace runspace)
    {
        var prox = runspace.SessionStateProxy;

        prox.SetVariable("PLUGINS", created(Path.GetFullPath("plugins")));

        prox.SetVariable("DOWNLOADS", created(Path.Combine(Init.ConfigDirectory, "downloads")));
        prox.SetVariable("LOCALAPPDATA", created(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)));
        // TODO: other


        string created(string path)
        {
            try { Directory.CreateDirectory(path); }
            catch { }

            return path;
        }
    }
}
