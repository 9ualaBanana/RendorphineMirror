using System.Diagnostics;
using System.Management.Automation.Runspaces;

namespace Node.Tasks.Exec;

public interface IPluginAction
{
    Type DataType { get; }
    PluginType Type { get; }
    string Name { get; }
    FileFormat FileFormat { get; }

    Task Execute(ReceivedTask task);
}
public abstract class PluginAction<T> : IPluginAction
{
    Type IPluginAction.DataType => typeof(T);

    public abstract string Name { get; }
    public abstract PluginType Type { get; }
    public abstract FileFormat FileFormat { get; }

    public async Task Execute(ReceivedTask task)
    {
        task.LogInfo($"Executing...");

        var data = task.Info.Data.ToObject<T>();
        if (data is null) throw new Exception("Could not deserialize input data: " + task.Info.Data);

        await Execute(task, data).ConfigureAwait(false);
        task.LogInfo($"Completed {Type} {Name} execution");
    }

    protected abstract Task Execute(ReceivedTask task, T data);


    protected static async ValueTask UploadResult(ReceivedTask task, ITaskOutput output, string resultfile)
    {
        await task.ChangeStateAsync(TaskState.Output);

        task.LogInfo($"Uploading output file {resultfile} to {task.Info.Output.ToString(Newtonsoft.Json.Formatting.None)} ...");
        await output.Upload(task, resultfile).ConfigureAwait(false);
        task.LogInfo($"Output file {resultfile} uploaded");

        await SendFileToReepo(task);
    }
    static async Task SendFileToReepo(ReceivedTask task, CancellationToken cancellationToken = default)
    {
        if (task.ExecuteLocally) return; // TODO: remove when local tasks go through the task manager

        var queryString = $"taskid={task.Id}&nodename={Settings.NodeName}";
        try { await Api.Client.PostAsync($"{Settings.ServerUrl}/tasks/result_preview?{queryString}", null, cancellationToken); }
        catch (Exception ex) { task.LogErr("Error sending result to reepo: " + ex); }
    }

    protected static Process StartProcess(string exepath, string args, ILoggable? logobj)
    {
        logobj?.LogInfo($"Starting {exepath} {args}");

        var process = Process.Start(new ProcessStartInfo(exepath, args) { RedirectStandardOutput = true, RedirectStandardError = true });
        if (process is null) throw new InvalidOperationException("Could not start plugin process");

        return process;
    }
    protected static void EnsureZeroStatusCode(Process process)
    {
        if (process.ExitCode != 0)
            throw new Exception($"Task process ended with exit code {process.ExitCode}");
    }
    protected static Task StartReadingProcessOutput(Process process, bool stderrToStdout, Action<bool, string>? onRead, ILoggable? logobj)
    {
        return Task.WhenAll(
            startReading(process.StandardOutput, false),
            startReading(process.StandardError, !stderrToStdout)
        );


        async Task startReading(StreamReader input, bool err)
        {
            while (true)
            {
                var str = await input.ReadLineAsync().ConfigureAwait(false);
                if (str is null) return;

                var logstr = $"[Process {process.Id}] {str}";
                if (err) logobj?.LogErr(logstr);
                else logobj?.LogInfo(logstr);

                onRead?.Invoke(err, str);
            }
        }
    }

    protected static async Task ExecuteProcess(string exepath, string args, bool stderrToStdout, Action<bool, string>? onRead, ILoggable? logobj)
    {
        using var process = StartProcess(exepath, args, logobj);
        var reading = StartReadingProcessOutput(process, stderrToStdout, onRead, logobj);

        await process.WaitForExitAsync();
        await reading;

        EnsureZeroStatusCode(process);
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

                onRead?.Invoke(false, item.ToString()!);
            }
        };
        pipeline.Error.DataReady += (obj, e) =>
        {
            foreach (var item in pipeline.Error.NonBlockingRead())
            {
                var logstr = $"[PowerShell {pipeline.GetHashCode()}] {item}";
                if (stderrToStdout) logobj?.LogInfo(logstr);
                else logobj?.LogErr(logstr);

                onRead?.Invoke(!stderrToStdout, item.ToString()!);
            }
        };

        pipeline.Commands.AddScript(script);
        var invoke = pipeline.Invoke();

        if (pipeline.PipelineStateInfo.Reason is not null)
            throw pipeline.PipelineStateInfo.Reason;

        // foreach (var err in pipeline..Streams.Error)
        //     throw err.Exception;
    }
}