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
        addvariables(runspace);

        var psh = PowerShell.Create(runspace);

        psh.AddStatement()
            .AddCommand("Import-Module")
            .AddParameter("Name", typeof(PowerShellInvoker).Assembly.Location);

        psh.AddStatement()
            .AddScript(script);

        return psh;


        static void addvariables(Runspace runspace)
        {
            var prox = runspace.SessionStateProxy;

            prox.SetVariable("PLUGINS", Directories.Created(Path.GetFullPath("plugins")));
            prox.SetVariable("DOWNLOADS", Directories.Created(Path.Combine(Init.ConfigDirectory, "downloads")));

            prox.SetVariable("LOCALAPPDATA", Directories.Created(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)));
        }
    }
    public static Collection<T> Invoke<T>(PowerShell psh)
    {
        var result = psh.Invoke<T>(Enumerable.Empty<object>(), new PSInvocationSettings() { ErrorActionPreference = ActionPreference.Stop });
        if (psh.InvocationStateInfo.Reason is not null)
            throw psh.InvocationStateInfo.Reason;

        foreach (var err in psh.Streams.Error)
            throw err.Exception;

        return result;
    }
    public static Collection<PSObject> Invoke(PowerShell psh) => Invoke<PSObject>(psh);

    public static Collection<PSObject> Invoke(string script) => Invoke(Initialize(script));

    public static Collection<PSObject> JustInvoke(string script) => Invoke(PowerShell.Create().AddScript(script));
    public static Collection<T> JustInvoke<T>(string script) => Invoke<T>(PowerShell.Create().AddScript(script));


    public static Collection<PSObject> Invoke(string script, Action<PSObject, Action>? onRead, Action<object, Action>? onErr, ILoggable? logobj, LogLevel? stdout = null, LogLevel? stderr = null)
    {
        var session = InitialSessionState.CreateDefault();
        session.Variables.Add(new SessionStateVariableEntry(nameof(PSInvocationSettings.ErrorActionPreference), nameof(ActionPreference.Stop), "Error action preference"));
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            session.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Unrestricted;

        using var runspace = RunspaceFactory.CreateRunspace(session);
        runspace.Open();

        using var pipeline = runspace.CreatePipeline();

        void process<T>(T item, LogLevel level, Action<T, Action>? action) =>
            action?.Invoke(item, () => logobj?.Log(level, $"[PowerShell {pipeline.GetHashCode()}] {item}"));
        pipeline.Output.DataReady += (obj, e) =>
        {
            foreach (var item in pipeline.Output.NonBlockingRead())
                process(item, stdout ?? LogLevel.Info, onRead);
        };
        pipeline.Error.DataReady += (obj, e) =>
        {
            foreach (var item in pipeline.Error.NonBlockingRead())
                process(item, stderr ?? LogLevel.Error, onErr);
        };

        pipeline.Commands.AddScript(script);
        var result = pipeline.Invoke();

        if (pipeline.PipelineStateInfo.Reason is not null)
            throw pipeline.PipelineStateInfo.Reason;

        return result;
    }
}
