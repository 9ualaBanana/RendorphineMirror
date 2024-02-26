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

        WatchingTasks.WatchingTasks.Bindable.SubscribeChanged(() => state.WatchingTasks.SetRange(WatchingTasks.WatchingTasks.Values), true);
        WatchingTasks.WatchingTasks.Bindable.SubscribeChanged(() =>
        {
            var oc = WatchingTasks.WatchingTasks.FirstOrDefault(t => t.Value.Source is OneClickWatchingTaskInputInfo).Value;
            if (oc is null) state.OneClickTaskInfo.Value = null;
            else
            {
                var source = (OneClickWatchingTaskInputInfo) oc.Source;
                state.OneClickTaskInfo.Value = new OneClickTaskInfo()
                {
                    IsPaused = oc.IsPaused,
                    InputDir = source.InputDirectory,
                    OutputDir = source.OutputDirectory,
                    ProductsDir = source.ProductsDirectory,
                    LogDir = source.OutputDirectory,
                    UnityTemplatesDir = @"C:\\OneClickUnityDefaultProjects",
                    ExportInfo = source.ExportInfo ?? [],
                };
            }
        }, true);

        RFProducts.RFProducts.Bindable.SubscribeChanged(() => state.RFProducts.SetRange(RFProducts.RFProducts.Select(p => KeyValuePair.Create(p.Key, JObject.FromObject(p.Value)))), true);
        PlacedTasks.PlacedTasks.Bindable.SubscribeChanged(() => state.PlacedTasks.SetRange(PlacedTasks.PlacedTasks.Values), true);
        CompletedTasks.CompletedTasks.Bindable.SubscribeChanged(() => state.CompletedTasks.SetRange(CompletedTasks.CompletedTasks.Values), true);
        QueuedTasks.QueuedTasks.Bindable.SubscribeChanged(() => state.QueuedTasks.SetRange(QueuedTasks.QueuedTasks.Values), true);
        Settings.BenchmarkResult.Bindable.SubscribeChanged(() => state.BenchmarkResult.Value = Settings.BenchmarkResult.Value is null ? null : JObject.FromObject(Settings.BenchmarkResult.Value), true);
        PluginManager.CachedPluginsBindable.SubscribeChanged(() => NodeGlobalState.InstalledPlugins.SetRange(PluginManager.CachedPluginsBindable.Value ?? Array.Empty<Plugin>()), true);

        Settings.TaskAutoDeletionDelayDays.Bindable.SubscribeChanged(() => state.TaskAutoDeletionDelayDays.Value = Settings.TaskAutoDeletionDelayDays.Value, true);
        Settings.BServerUrl.Bindable.SubscribeChanged(() => state.ServerUrl.Value = Settings.BServerUrl.Bindable.Value, true);
        Settings.BLocalListenPort.Bindable.SubscribeChanged(() => state.LocalListenPort.Value = Settings.BLocalListenPort.Bindable.Value, true);
        Settings.BUPnpPort.Bindable.SubscribeChanged(() => state.UPnpPort.Value = Settings.BUPnpPort.Bindable.Value, true);
        Settings.BUPnpServerPort.Bindable.SubscribeChanged(() => state.UPnpServerPort.Value = Settings.BUPnpServerPort.Bindable.Value, true);
        Settings.BDhtPort.Bindable.SubscribeChanged(() => state.DhtPort.Value = Settings.BDhtPort.Bindable.Value, true);
        Settings.BTorrentPort.Bindable.SubscribeChanged(() => state.TorrentPort.Value = Settings.BTorrentPort.Bindable.Value, true);
        Settings.BNodeName.Bindable.SubscribeChanged(() => state.NodeName.Value = Settings.BNodeName.Bindable.Value, true);
        Settings.BAuthInfo.Bindable.SubscribeChanged(() => state.AuthInfo.Value = Settings.BAuthInfo.Bindable.Value, true);
        Settings.AcceptTasks.Bindable.SubscribeChanged(() => state.AcceptTasks.Value = Settings.AcceptTasks.Bindable.Value, true);
        Settings.TaskProcessingDirectory.Bindable.SubscribeChanged(() => state.TaskProcessingDirectory.Value = Settings.TaskProcessingDirectory.Bindable.Value, true);

        Settings.MPlusUsername.Bindable.SubscribeChanged(() => state.MPlusUsername.Value = Settings.MPlusUsername.Bindable.Value, true);
        Settings.MPlusPassword.Bindable.SubscribeChanged(() => state.MPlusPassword.Value = Settings.MPlusPassword.Bindable.Value, true);
        Settings.TurboSquidUsername.Bindable.SubscribeChanged(() => state.TurboSquidUsername.Value = Settings.TurboSquidUsername.Bindable.Value, true);
        Settings.TurboSquidPassword.Bindable.SubscribeChanged(() => state.TurboSquidPassword.Value = Settings.TurboSquidPassword.Bindable.Value, true);
        Settings.CGTraderUsername.Bindable.SubscribeChanged(() => state.CGTraderUsername.Value = Settings.CGTraderUsername.Bindable.Value, true);
        Settings.CGTraderPassword.Bindable.SubscribeChanged(() => state.CGTraderPassword.Value = Settings.CGTraderPassword.Bindable.Value, true);

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
