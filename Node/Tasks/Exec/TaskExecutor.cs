using System.Diagnostics;
using Machine.Plugins.Plugins;

namespace Node.Tasks.Exec;

public record TaskExecuteData(string Input, string Output, string Id, IPluginAction Action, Plugin Plugin)
{
    public void LogInfo(string text) => Log.Information($"[Task {Id}] {text}");
    public void LogErr(string text) => Log.Error($"[Task {Id}] {text}");
    public void LogErr(Exception ex) => LogErr(ex.ToString());

    public void LogExecInfo(string text) => Log.Information($"[TaskExec {Id}] {text}");
    public void LogExecErr(string text) => Log.Error($"[TaskExec {Id}] {text}");
}

public abstract class TaskExecutor<TBase> : ITaskExecutor
{
    public abstract IEnumerable<IPluginAction> GetTasks();

    protected async ValueTask<string> Start<T>(TaskExecuteData task, T data) where T : TBase =>
        await Execute(task, data).ConfigureAwait(false);

    protected abstract Task<string> Execute(TaskExecuteData task, TBase data);

    protected void StartReadingProcessOutput(TaskExecuteData task, Process process)
    {
        startReading(process.StandardOutput, false).Consume();
        startReading(process.StandardError, true).Consume();


        async Task startReading(StreamReader input, bool err)
        {
            while (true)
            {
                var str = await input.ReadLineAsync().ConfigureAwait(false);
                if (str is null) return;

                if (err) task.LogExecErr(str);
                else task.LogExecInfo(str);
            }
        }
    }
}
public abstract class ProcessTaskExecutor<TBase> : TaskExecutor<TBase>
{
    protected sealed override async Task<string> Execute(TaskExecuteData task, TBase data)
    {
        var args = GetArguments(task, data);

        var exepath = GetExecutable(task);
        task.LogInfo($"Starting {exepath} {args}");

        var process = Process.Start(new ProcessStartInfo(exepath, args) { RedirectStandardOutput = true, RedirectStandardError = true });
        if (process is null) throw new InvalidOperationException("Could not start plugin process");

        StartReadingProcessOutput(task, process);


        await process.WaitForExitAsync().ConfigureAwait(false);
        var ret = process.ExitCode;
        if (ret != 0)
        {
            var err = process.StandardOutput.ReadToEnd();
            if (err.Length == 0) err = "unknown error";

            throw new Exception($"Could not complete task: {err} (exit code {ret})");
        }

        task.LogInfo($"Completed {task.Plugin.Type} {task.Action.Type} execution");
        return task.Output;
    }

    protected virtual string GetExecutable(TaskExecuteData task) => task.Plugin.Path;
    protected virtual void AfterExecution(Exception? exception) { }

    protected abstract string GetArguments(TaskExecuteData task, TBase data);
}