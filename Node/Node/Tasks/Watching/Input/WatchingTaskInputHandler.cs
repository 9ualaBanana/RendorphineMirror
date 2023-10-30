namespace Node.Tasks.Watching.Input;

public interface IWatchingTaskInputHandler
{
    void StartListening();
    void OnCompleted(DbTaskFullState task);
}

public abstract class WatchingTaskInputHandler<TInput> : IWatchingTaskInputHandler
    where TInput : IWatchingTaskInputInfo
{
    public CancellationTokenSource Token { get; } = new();

    public required IWatchingTasksStorage WatchingTasks { get; init; }
    public required NodeTaskRegistration TaskRegistration { get; init; }
    public required WatchingTask Task { get; init; }
    public required ILogger<WatchingTaskInputHandler<TInput>> Logger { get; init; }

    protected TInput Input => (TInput) Task.Source;

    protected void StartThreadRepeated(int msrepeat, Func<Task> action)
    {
        new Thread(async () =>
        {
            while (true)
            {
                if (Token.IsCancellationRequested) return;

                if (!Task.IsPaused)
                {
                    try { await action(); }
                    catch (Exception ex) { Logger.LogError(ex, ""); }
                }

                await System.Threading.Tasks.Task.Delay(msrepeat);
            }
        })
        {
            Name = $"Watching task handler for task {Task.Id}",
            IsBackground = true
        }.Start();
    }

    public virtual void OnCompleted(DbTaskFullState task) { }

    protected void SaveTask() => WatchingTasks.WatchingTasks.Save(Task);

    public abstract void StartListening();
}
