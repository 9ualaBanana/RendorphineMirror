namespace Node.Services.Targets;

public class NodeGlobalStateInitializedTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder)
    {
        builder.RegisterType<NodeGui>()
            .AsSelf()
            .AsImplementedInterfaces()
            .SingleInstance();

        builder.RegisterInstance(NodeGlobalState.Instance)
            .SingleInstance();
    }

    public required TaskListTarget TaskList { get; init; }
    public required SettingsInstance Settings { get; init; }
    public required NodeGlobalState NodeGlobalState { get; init; }
    public required PluginManager PluginManager { get; init; }
    public required IWatchingTasksStorage WatchingTasks { get; init; }
    public required IRFProductStorage RFProducts { get; init; }
    public required IQueuedTasksStorage QueuedTasks { get; init; }
    public required IPlacedTasksStorage PlacedTasks { get; init; }
    public required ICompletedTasksStorage CompletedTasks { get; init; }
    public required NodeStateSender StateSender { get; init; }
    public required Updaters.BalanceUpdater BalanceUpdater { get; init; }
    public required Updaters.SoftwareUpdater SoftwareUpdater { get; init; }
    public required Updaters.SoftwareStatsUpdater SoftwareStatsUpdater { get; init; }
    public required IComponentContext Container { get; init; }
    public required ILogger<NodeGlobalStateInitializedTarget> Logger { get; init; }

    async Task IServiceTarget.ExecuteAsync()
    {
        var state = NodeGlobalState;

        state.WatchingTasks.BindOneWayFrom(WatchingTasks.WatchingTasks.Bindable);
        RFProducts.RFProducts.Bindable.SubscribeChanged(() => state.RFProducts.SetRange(RFProducts.RFProducts.Select(p => KeyValuePair.Create(p.Key, JObject.FromObject(p.Value)))), true);
        state.PlacedTasks.BindOneWayFrom(PlacedTasks.PlacedTasks.Bindable);
        state.CompletedTasks.BindOneWayFrom(CompletedTasks.CompletedTasks.Bindable);
        QueuedTasks.QueuedTasks.Bindable.SubscribeChanged(() => state.QueuedTasks.SetRange(QueuedTasks.QueuedTasks.Values), true);
        Settings.BenchmarkResult.Bindable.SubscribeChanged(() => state.BenchmarkResult.Value = Settings.BenchmarkResult.Value is null ? null : JObject.FromObject(Settings.BenchmarkResult.Value), true);
        PluginManager.CachedPluginsBindable.SubscribeChanged(() => NodeGlobalState.InstalledPlugins.SetRange(PluginManager.CachedPluginsBindable.Value ?? Array.Empty<Plugin>()), true);
        state.TaskAutoDeletionDelayDays.BindOneWayFrom(Settings.TaskAutoDeletionDelayDays.Bindable);

        state.ServerUrl.BindOneWayFrom(Settings.BServerUrl.Bindable);
        state.LocalListenPort.BindOneWayFrom(Settings.BLocalListenPort.Bindable);
        state.UPnpPort.BindOneWayFrom(Settings.BUPnpPort.Bindable);
        state.UPnpServerPort.BindOneWayFrom(Settings.BUPnpServerPort.Bindable);
        state.DhtPort.BindOneWayFrom(Settings.BDhtPort.Bindable);
        state.TorrentPort.BindOneWayFrom(Settings.BTorrentPort.Bindable);
        state.NodeName.BindOneWayFrom(Settings.BNodeName.Bindable);
        state.AuthInfo.BindOneWayFrom(Settings.BAuthInfo.Bindable);
        state.AcceptTasks.BindOneWayFrom(Settings.AcceptTasks.Bindable);
        state.TaskProcessingDirectory.BindOneWayFrom(Settings.TaskProcessingDirectory.Bindable);

        await Task.WhenAll([
            BalanceUpdater.Start(null, state.Balance, default),
            SoftwareUpdater.Start(null, state.Software, default),
            SoftwareStatsUpdater.Start(null, state.SoftwareStats, default),
        ]);


        state.TaskDefinitions.Value = serializeActions();
        TasksFullDescriber serializeActions()
        {
            return new TasksFullDescriber(
                TaskList.Actions.Select(serializeaction).ToImmutableArray(),
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
