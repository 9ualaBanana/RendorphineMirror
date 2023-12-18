namespace Node.Services.Targets;

public class TaskExecutorTarget : IServiceTarget
{
    public const string TaskExecutionScope = "taskexecution";

    public static void CreateRegistrations(ContainerBuilder builder)
    {
        builder.RegisterType<PlacedTasksHandler>()
            .SingleInstance();
        builder.RegisterType<ReceivedTasksHandler>()
            .SingleInstance();
        builder.RegisterType<WatchingTasksHandler>()
            .SingleInstance();

        builder.RegisterType<TaskExecutor>()
            .AsSelf()
            .AsImplementedInterfaces()
            .SingleInstance();

        builder.RegisterType<RFProduct.ID_.Generator>()
    .SingleInstance();

        builder.RegisterType<RFProduct.Factory>()
            .AsSelf()
            .SingleInstance();

        builder.RegisterType<RFProduct.Video.Constructor>()
            .AsSelf()
            .SingleInstance();

        builder.RegisterType<RFProduct.Image.Constructor>()
            .AsSelf()
            .SingleInstance();

        builder.RegisterType<RFProduct.Image.QSPreviews.Generator>()
            .AsSelf()
            .SingleInstance();

        builder.RegisterType<RFProduct.Video.QSPreviews.Generator>()
            .AsSelf()
            .SingleInstance();
    }

    public required TaskListTarget TaskList { get; init; }
    public required ReconnectTarget Reconnect { get; init; }
    public required PlacedTasksHandler PlacedTasksHandler { get; init; }
    public required ReceivedTasksHandler ReceivedTasksHandler { get; init; }
    public required WatchingTasksHandler WatchingTasksHandler { get; init; }
    public required TaskExecutor TaskExecutor { get; init; }

    public required ICompletedTasksStorage CompletedTasks { get; init; }
    public required IWatchingTasksStorage WatchingTasks { get; init; }
    public required IQueuedTasksStorage QueuedTasks { get; init; }
    public required IPlacedTasksStorage PlacedTasks { get; init; }
    public required ILogger<TaskExecutorTarget> Logger { get; init; }

    async Task IServiceTarget.ExecuteAsync()
    {
        Logger.Info($"""
            Tasks found
            {CompletedTasks.CompletedTasks.Count} self-completed
            {WatchingTasks.WatchingTasks.Count} watching
            {QueuedTasks.QueuedTasks.Count} queued
            {PlacedTasks.PlacedTasks.Count} placed
            {PlacedTasks.PlacedTasks.Values.Count(x => !x.State.IsFinished())} non-finished placed
            """.Replace("\n", "; ").Replace("\r", string.Empty));
    }
    void IServiceTarget.Activated()
    {
        PlacedTasksHandler.InitializePlacedTasksAsync().Consume();
        PlacedTasksHandler.StartUpdatingPlacedTasks();
        WatchingTasksHandler.StartWatchingTasks();
        ReceivedTasksHandler.StartListening();
    }
}
