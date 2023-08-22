using Node.Tasks.Exec.Actions;

namespace Node.Services.Targets;

public class TaskExecutorTarget : IServiceTarget
{
    public const string TaskExecutionScope = "taskexecution";

    public static void CreateRegistrations(ContainerBuilder builder)
    {
        builder.RegisterType<TaskHandler>()
            .SingleInstance();

        IOList.RegisterAll(builder);
        registerTasks();


        void registerTasks()
        {
            void register<T>() where T : IPluginActionInfo =>
                builder.RegisterType<T>()
                    .Keyed<IPluginActionInfo>(Enum.Parse<TaskAction>(typeof(T).Name))
                    .SingleInstance();

            register<EditRaster>();
            register<EditVideo>();
            register<EsrganUpscale>();
            register<GreenscreenBackground>();
            register<VeeeVectorize>();
            register<GenerateQSPreview>();
            register<GenerateTitleKeywords>();
            //registertask<GenerateImageByMeta>();
            register<GenerateImageByPrompt>();
            register<Topaz>();
        }
    }

    public required TaskHandler TaskHandler { get; init; }

    public required ICompletedTasksStorage CompletedTasks { get; init; }
    public required IWatchingTasksStorage WatchingTasks { get; init; }
    public required IQueuedTasksStorage QueuedTasks { get; init; }
    public required IPlacedTasksStorage PlacedTasks { get; init; }
    public required ILogger<TaskExecutorTarget> Logger { get; init; }

    public async Task ExecuteAsync()
    {
        TaskHandler.InitializePlacedTasksAsync().Consume();
        TaskHandler.StartUpdatingPlacedTasks();
        TaskHandler.StartWatchingTasks();
        TaskHandler.StartListening();

        Logger.Info($"""
            Tasks found
            {CompletedTasks.CompletedTasks.Count} self-completed
            {WatchingTasks.WatchingTasks.Count} watching
            {QueuedTasks.QueuedTasks.Count} queued
            {PlacedTasks.PlacedTasks.Count} placed
            {PlacedTasks.PlacedTasks.Values.Count(x => !x.State.IsFinished())} non-finished placed
            """.Replace("\n", "; ").Replace("\r", string.Empty));
    }
}
