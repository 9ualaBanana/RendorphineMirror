using System.Diagnostics;

namespace Node.Tasks.Exec;

public abstract class TaskExecutor<TBase> : ITaskExecutor
{
    public abstract IEnumerable<IPluginAction> GetTasks();

    protected async ValueTask<string[]> Start<T>(string[] files, ReceivedTask task, T data) where T : TBase =>
        await Task.WhenAll(files.Select(x => Execute(x, task, data))).ConfigureAwait(false);

    protected abstract Task<string> Execute(string input, ReceivedTask task, TBase data);

    protected void StartReadingProcessOutput(ReceivedTask task, Process process)
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
    protected sealed override async Task<string> Execute(string input, ReceivedTask task, TBase data)
    {
        var output = Path.Combine(Init.TaskFilesDirectory, task.Id, Path.GetFileNameWithoutExtension(input) + "_out" + Path.GetExtension(input));
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);

        var args = GetArguments(input, output, task, data);

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

        task.LogInfo($"Completed {task.GetAction().Type} {task.Info.TaskType} execution");
        return output;
    }

    protected virtual string GetExecutable(ReceivedTask task) => task.GetPlugin().Path;
    protected virtual void AfterExecution(Exception? exception) { }

    protected abstract string GetArguments(string input, string output, ReceivedTask task, TBase data);
}