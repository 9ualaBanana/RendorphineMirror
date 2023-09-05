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
    public required ILogger<NodeGlobalStateInitializedTarget> Logger { get; init; }

    public Task ExecuteAsync()
    {
        var state = NodeGlobalState;

        state.WatchingTasks.Bind(NodeSettings.WatchingTasks.Bindable);
        state.PlacedTasks.Bind(NodeSettings.PlacedTasks.Bindable);
        NodeSettings.QueuedTasks.Bindable.SubscribeChanged(() => state.QueuedTasks.SetRange(NodeSettings.QueuedTasks.Values), true);
        Settings.BenchmarkResult.Bindable.SubscribeChanged(() => state.BenchmarkResult.Value = Settings.BenchmarkResult.Value is null ? null : JObject.FromObject(Settings.BenchmarkResult.Value), true);
        PluginManager.CachedPluginsBindable.SubscribeChanged(() => NodeGlobalState.InstalledPlugins.SetRange(PluginManager.CachedPluginsBindable.Value ?? Array.Empty<Plugin>()), true);
        state.TaskAutoDeletionDelayDays.Bind(Settings.TaskAutoDeletionDelayDays.Bindable);

        state.BServerUrl.Bind(Settings.BServerUrl.Bindable);
        state.BLocalListenPort.Bind(Settings.BLocalListenPort.Bindable);
        state.BUPnpPort.Bind(Settings.BUPnpPort.Bindable);
        state.BUPnpServerPort.Bind(Settings.BUPnpServerPort.Bindable);
        state.BDhtPort.Bind(Settings.BDhtPort.Bindable);
        state.BTorrentPort.Bind(Settings.BTorrentPort.Bindable);
        state.BNodeName.Bind(Settings.BNodeName.Bindable);
        state.BAuthInfo.Bind(Settings.BAuthInfo.Bindable);


        Software.StartUpdating(null, Logger, default);
        Settings.BLocalListenPort.Bindable.SubscribeChanged(() => File.WriteAllText(Path.Combine(Directories.Data, "lport"), Settings.LocalListenPort.ToString()), true);


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



        return Task.CompletedTask;
    }
}
