using Newtonsoft.Json;

namespace Node.Tasks.Watching;

public abstract class WatchingTaskHandler<TInput> : IWatchingTaskInputHandler where TInput : IWatchingTaskInputInfo
{
    public abstract WatchingTaskInputType Type { get; }
    protected readonly CancellationTokenSource CancellationToken = new();

    [JsonIgnore] public WatchingTask Task { get; }
    [JsonIgnore] protected TInput Input => (TInput) Task.Source;

    protected WatchingTaskHandler(WatchingTask task) => Task = task;


    public abstract void StartListening();

    protected Thread StartThreadRepeated(int msrepeat, Func<ValueTask> action)
    {
        var thread = new Thread(async () =>
        {
            while (true)
            {
                if (CancellationToken.IsCancellationRequested) return;

                if (!Task.IsPaused)
                {
                    try { await action(); }
                    catch (Exception ex) { Task.LogErr(ex); }
                }

                await System.Threading.Tasks.Task.Delay(msrepeat);
            }
        });
        thread.Name = $"Watching task handler for task {Task.Id}, {Type}";
        thread.IsBackground = true;
        thread.Start();

        return thread;
    }
    protected void SaveTask() => NodeSettings.WatchingTasks.Save(Task);


    protected virtual void Dispose() { }

    bool Disposed;
    void IDisposable.Dispose()
    {
        if (Disposed) return;

        CancellationToken.Cancel();
        Dispose();
        GC.SuppressFinalize(this);

        Disposed = true;
    }
}
