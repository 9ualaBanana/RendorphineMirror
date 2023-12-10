namespace Node.Tasks.Exec;

public class WatchingTasksHandler
{
    public required ILifetimeScope Container { get; init; }
    public required IWatchingTasksStorage WatchingTasks { get; init; }
    public required ILogger<WatchingTasksHandler> Logger { get; init; }

    readonly Dictionary<WatchingTask, IWatchingTaskInputHandler> Running = new();

    public IWatchingTaskInputHandler GetHandler(WatchingTask task) => Running[task];
    public T GetHandler<T>(WatchingTask task) where T : IWatchingTaskInputHandler => (T) GetHandler(task);

    public void StartWatchingTasks()
    {
        foreach (var task in WatchingTasks.WatchingTasks.Values)
            StartWatchingTask(task);
    }
    public void StartWatchingTask(WatchingTask task)
    {
        using var _logscope = Logger.BeginScope($"WTask {(task.Id.Length > 5 ? $"{task.Id.Substring(0, 5)}.." : task.Id)}");
        Logger.LogInformation($"Watcher started; Data: {JsonConvert.SerializeObject(task)}");

        var handler = CreateWatchingHandler(task);
        handler.StartListening();
        task.OnCompleted += handler.OnCompleted;
        Running.Add(task, handler);


        IWatchingTaskInputHandler CreateWatchingHandler(WatchingTask task)
        {
            var scope = Container.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(task)
                    .SingleInstance();
            });

            return scope.ResolveKeyed<IWatchingTaskInputHandler>(task.Source.Type);
        }
    }
}
