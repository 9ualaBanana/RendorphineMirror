using System.Diagnostics;
using System.Management.Automation.Runspaces;

namespace Node.Tasks.Exec;

public interface IPluginAction
{
    Type DataType { get; }
    PluginType Type { get; }
    TaskAction Name { get; }
    IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats { get; }

    Task Execute(ReceivedTask task);
}
public abstract class PluginAction<T> : IPluginAction
{
    Type IPluginAction.DataType => typeof(T);

    public abstract TaskAction Name { get; }
    public abstract PluginType Type { get; }
    protected Plugin PluginInstance => Type.GetInstance();
    protected string PluginPath => PluginInstance.Path;

    public abstract IReadOnlyCollection<IReadOnlyCollection<FileFormat>> InputFileFormats { get; }
    protected abstract OperationResult ValidateOutputFiles(ReceivedTask task, T data);

    protected void ValidateInputFilesThrow(ReceivedTask task) =>
        ValidateInputFiles(task).ThrowIfError($"Task {task.Id} input file validation failed: {{0}}");
    OperationResult ValidateInputFiles(ReceivedTask task) => TaskRequirement.EnsureInputFormats(task, InputFileFormats);

    public async Task Execute(ReceivedTask task)
    {
        task.LogInfo($"Executing...");

        var data = task.Info.Data.ToObject<T>();
        if (data is null) throw new Exception("Could not deserialize input data: " + task.Info.Data);

        await Execute(task, data).ConfigureAwait(false);
        task.LogInfo($"Completed {Type} {Name} execution");
    }

    protected abstract Task Execute(ReceivedTask task, T data);


    /// <summary>
    /// For windows, just returns provided path parameter.
    /// For unix, returns path for use in wine: /home/user/test -> Z:\home\user\test
    /// </summary>
    protected static async ValueTask<string> GetWinPath(string path)
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT) return path;
        return await Processes.FullExecute("/bin/winepath", new[] { "-w", path }, null, LogLevel.Info, LogLevel.Error);
    }
    protected static Task ExecuteProcessWithWineSupport(string exepath, string args, Action<bool, string>? onRead, ILoggable? logobj, LogLevel? stdout = null, LogLevel? stderr = null)
    {
        if (Environment.OSVersion.Platform == PlatformID.Unix && exepath.EndsWith(".exe", StringComparison.Ordinal))
        {
            args = $"\"{exepath}\" {args}";
            exepath = "/bin/wine";
        }

        return Processes.Execute(exepath, args, onRead, logobj, stdout, stderr);
    }


    protected Task ExecutePowerShellAtWithCondaEnvAsync(ReceivedTask task, string script, bool stderrToStdout, Action<bool, object>? onRead) =>
        Task.Run(() => ExecutePowerShellAtWithCondaEnv(task, script, stderrToStdout, onRead));
    protected void ExecutePowerShellAtWithCondaEnv(ReceivedTask task, string script, bool stderrToStdout, Action<bool, object>? onRead)
    {
        var plugin = PluginInstance;
        script = $"""
            Set-Location '{Path.GetFullPath(Path.GetDirectoryName(plugin.Path)!)}'
            {script}
            """;
        script = CondaManager.WrapWithInitEnv($"{plugin.Type}_{plugin.Version}", script);

        ExecutePowerShell(script, stderrToStdout, onRead, task);
    }
    protected static void ExecutePowerShell(string script, bool stderrToStdout, Action<bool, object>? onRead, ILoggable? logobj)
    {
        var session = InitialSessionState.CreateDefault();
        session.Variables.Add(new SessionStateVariableEntry("ErrorActionPreference", "Stop", "Error action preference"));
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            session.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Unrestricted;

        using var runspace = RunspaceFactory.CreateRunspace(session);
        runspace.Open();

        using var pipeline = runspace.CreatePipeline();

        pipeline.Output.DataReady += (obj, e) =>
        {
            foreach (var item in pipeline.Output.NonBlockingRead())
            {
                var logstr = $"[PowerShell {pipeline.GetHashCode()}] {item}";
                logobj?.LogInfo(logstr);

                onRead?.Invoke(false, item);
            }
        };
        pipeline.Error.DataReady += (obj, e) =>
        {
            foreach (var item in pipeline.Error.NonBlockingRead())
            {
                var logstr = $"[PowerShell {pipeline.GetHashCode()}] {item}";
                if (stderrToStdout) logobj?.LogInfo(logstr);
                else logobj?.LogErr(logstr);

                onRead?.Invoke(!stderrToStdout, item);
            }
        };

        pipeline.Commands.AddScript(script);
        LogManager.GetLogger("amogus").Trace(script);
        var invoke = pipeline.Invoke();

        if (pipeline.PipelineStateInfo.Reason is not null)
            throw pipeline.PipelineStateInfo.Reason;

        // foreach (var err in pipeline..Streams.Error)
        //     throw err.Exception;
    }
}