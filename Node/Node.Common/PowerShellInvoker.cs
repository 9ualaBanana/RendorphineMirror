using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;

namespace Node.Common;

[AutoRegisteredService(true)]
public class PowerShellInvoker
{
    public required DataDirs Dirs { get; init; }
    public required ILogger<PowerShellInvoker> Logger { get; init; }

    public PowerShell Initialize(string script)
    {
        var runspace = RunspaceFactory.CreateRunspace();
        // TODO: restrictions on commands?


        runspace.Open();
        addvariables(runspace);

        var psh = PowerShell.Create(runspace);

        // TODO:: do somenthing about imports
        psh.AddStatement()
            .AddCommand("Import-Module")
            .AddParameter("Name", typeof(PowerShellInvoker).Assembly.Location);
        psh.AddStatement()
            .AddCommand("Import-Module")
            .AddParameter("Name", Assembly.GetEntryAssembly().ThrowIfNull().Location);

        psh.AddStatement()
            .AddScript(script);

        return psh;


        void addvariables(Runspace runspace)
        {
            var prox = runspace.SessionStateProxy;

            prox.SetVariable("DOWNLOADS", Dirs.NamedTempDir("plugindl"));
            prox.SetVariable("LOCALAPPDATA", Directories.DirCreated(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)));
        }
    }

    public Collection<T> Invoke<T>(PowerShell psh) => Invoke<T>(psh, Logger);
    public static Collection<T> Invoke<T>(PowerShell psh, ILogger logger)
    {
        var result = psh.Invoke<T>(Enumerable.Empty<object>(), new PSInvocationSettings() { ErrorActionPreference = ActionPreference.Stop });
        if (psh.InvocationStateInfo.Reason is not null)
            throw psh.InvocationStateInfo.Reason;

        foreach (var err in psh.Streams.Error)
        {
            logger.LogTrace($"Powershell ended with errors {JsonConvert.SerializeObject(err)}");
            throw err.Exception;
        }

        return result;
    }

    public Collection<PSObject> Invoke(PowerShell psh) => Invoke<PSObject>(psh);
    public Collection<PSObject> Invoke(string script) => Invoke(Initialize(script));

    public Collection<PSObject> JustInvoke(string script) => Invoke(PowerShell.Create().AddScript(script));
    public Collection<T> JustInvoke<T>(string script) => Invoke<T>(PowerShell.Create().AddScript(script));

    public static Collection<T> JustInvoke<T>(string script, ILogger logger) => Invoke<T>(PowerShell.Create().AddScript(script), logger);


    public Collection<PSObject> Invoke(string script, Action<PSObject, Action>? onRead, Action<object, Action>? onErr, LogLevel? stdout = null, LogLevel? stderr = null)
    {
        Logger.LogTrace($"Invoking powershell script: `\n{script}\n`");

        var session = InitialSessionState.CreateDefault();
        session.Variables.Add(new SessionStateVariableEntry(nameof(PSInvocationSettings.ErrorActionPreference), nameof(ActionPreference.Stop), "Error action preference"));
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            session.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Unrestricted;

        using var runspace = RunspaceFactory.CreateRunspace(session);
        runspace.Open();

        using var pipeline = runspace.CreatePipeline();

        void process<T>(T item, LogLevel level, Action<T, Action>? action) =>
            action?.Invoke(item, () => Logger.Log(level, $"[PowerShell {pipeline.GetHashCode()}] {item}"));
        pipeline.Output.DataReady += (obj, e) =>
        {
            foreach (var item in pipeline.Output.NonBlockingRead())
                process(item, stdout ?? LogLevel.Trace, onRead);
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
