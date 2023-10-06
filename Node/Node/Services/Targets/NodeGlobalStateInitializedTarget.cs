namespace Node.Services.Targets;

public class NodeGlobalStateInitializedTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder)
    {
        builder.RegisterInstance(NodeGlobalState.Instance)
            .SingleInstance();
    }

    public required NodeGlobalState NodeGlobalState { get; init; }
    public required PluginManager PluginManager { get; init; }
    public required IIndex<TaskAction, IPluginActionInfo> Actions { get; init; }
    public required IWatchingTasksStorage WatchingTasks { get; init; }
    public required IQueuedTasksStorage QueuedTasks { get; init; }
    public required IPlacedTasksStorage PlacedTasks { get; init; }
    public required ICompletedTasksStorage CompletedTasks { get; init; }
    public required NodeStateSender StateSender { get; init; }
    public required Updaters.BalanceUpdater BalanceUpdater { get; init; }
    public required Updaters.SoftwareUpdater SoftwareUpdater { get; init; }
    public required Updaters.SoftwareStatsUpdater SoftwareStatsUpdater { get; init; }
    public required ILogger<NodeGlobalStateInitializedTarget> Logger { get; init; }

    public async Task ExecuteAsync()
    {
        var state = NodeGlobalState;

        state.WatchingTasks.Bind(WatchingTasks.WatchingTasks.Bindable);
        state.PlacedTasks.Bind(PlacedTasks.PlacedTasks.Bindable);
        state.CompletedTasks.Bind(CompletedTasks.CompletedTasks.Bindable);
        QueuedTasks.QueuedTasks.Bindable.SubscribeChanged(() => state.QueuedTasks.SetRange(QueuedTasks.QueuedTasks.Values), true);
        Settings.BenchmarkResult.Bindable.SubscribeChanged(() => state.BenchmarkResult.Value = Settings.BenchmarkResult.Value is null ? null : JObject.FromObject(Settings.BenchmarkResult.Value), true);
        PluginManager.CachedPluginsBindable.SubscribeChanged(() => NodeGlobalState.InstalledPlugins.SetRange(PluginManager.CachedPluginsBindable.Value ?? Array.Empty<Plugin>()), true);
        state.TaskAutoDeletionDelayDays.Bind(Settings.TaskAutoDeletionDelayDays.Bindable);

        state.ServerUrl.Bind(Settings.BServerUrl.Bindable);
        state.LocalListenPort.Bind(Settings.BLocalListenPort.Bindable);
        state.UPnpPort.Bind(Settings.BUPnpPort.Bindable);
        state.UPnpServerPort.Bind(Settings.BUPnpServerPort.Bindable);
        state.DhtPort.Bind(Settings.BDhtPort.Bindable);
        state.TorrentPort.Bind(Settings.BTorrentPort.Bindable);
        state.NodeName.Bind(Settings.BNodeName.Bindable);
        state.AuthInfo.Bind(Settings.BAuthInfo.Bindable);

        await Task.WhenAll(new[]
        {
            BalanceUpdater.Start(null, state.Balance, default),
            SoftwareUpdater.Start(null, state.Software, default),
            SoftwareStatsUpdater.Start(null, state.SoftwareStats, default),
        });


        state.TaskDefinitions.Value = serializeActions();
        TasksFullDescriber serializeActions()
        {
            return new TasksFullDescriber(
                Enum.GetValues<TaskAction>()
                    .Select(type => Actions.TryGetValue(type, out var info) ? info : null)
                    .WhereNotNull()
                    .Select(serializeaction)
                    .ToImmutableArray(),
                serialize(TaskModels.Inputs),
                serialize(TaskModels.Outputs),
                serialize(TaskModels.WatchingInputs),
                serialize(TaskModels.WatchingOutputs)
            );


            static TaskActionDescriber serializeaction(IPluginActionInfo action) => new TaskActionDescriber(action.RequiredPlugins, action.Name.ToString(), new ObjectDescriber(action.DataType));
            static ImmutableArray<TaskInputOutputDescriber> serialize<T>(ImmutableDictionary<T, Type> dict) where T : struct, Enum =>
                dict.Select(x => new TaskInputOutputDescriber(x.Key.ToString(), new ObjectDescriber(x.Value))).ToImmutableArray();
        }
    }
}
